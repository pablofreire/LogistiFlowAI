using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Application.Options;
using LogistiFlow.AiOperations.Application.Tools;
using LogistiFlow.AiOperations.Domain.Enums;
using LogistiFlow.AiOperations.UnitTests.Fixtures;
using NSubstitute;

namespace LogistiFlow.AiOperations.UnitTests.Application.Tools;

public sealed class OperationsToolsetTests
{
    private readonly IOrdersApi _orders = Substitute.For<IOrdersApi>();
    private readonly IInventoryApi _inventory = Substitute.For<IInventoryApi>();
    private readonly IFulfillmentApi _fulfillment = Substitute.For<IFulfillmentApi>();
    private readonly IShippingApi _shipping = Substitute.For<IShippingApi>();
    private readonly CopilotToolCallLog _log = new();
    private readonly FakeClock _clock = new(OperationsData.Now);
    private readonly OperationsToolset _tools;

    public OperationsToolsetTests() =>
        _tools = new OperationsToolset(_orders, _inventory, _fulfillment, _shipping, _log, Microsoft.Extensions.Options.Options.Create(new CopilotOptions()), _clock);

    [Theory]
    [InlineData("1025")]
    [InlineData("ORD-")]
    [InlineData("ORD-12'; DROP TABLE orders; --")]
    public async Task InvalidOrderNumber_IsRejected_WithoutCallingAnyService(string orderNumber)
    {
        var result = await _tools.AnalyzeShipmentDelayAsync(orderNumber, CancellationToken.None);

        result.Status.Should().Be("invalid");
        result.Data.Should().BeNull();
        await _orders.DidNotReceiveWithAnyArgs().FindByNumberAsync(default!, default);
        _log.Calls.Should().ContainSingle().Which.Outcome.Should().Be(CopilotToolCallOutcome.Invalid);
    }

    [Fact]
    public async Task OrderNumber_IsNormalised()
    {
        _orders.FindByNumberAsync("ORD-1025", Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(OperationsData.Order()));
        _orders.GetHistoryAsync(OperationsData.OrderId, Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(OperationsData.History()));

        var result = await _tools.GetOrderDetailsAsync(" ord-1025 ", CancellationToken.None);

        result.Status.Should().Be("ok");
        result.Data!.History.Should().HaveCount(2);
        _log.Calls.Single().Summary.Should().Be("ORD-1025: Shipped");
    }

    [Fact]
    public async Task Exceptions_AreLimitedToTheWindow_AndCountedByTypeAndStatus()
    {
        var recent = OperationsData.Delay();
        var old = OperationsData.Delay("Resolved") with { OpenedAt = OperationsData.Now.AddHours(-30) };
        var outage = recent with { Type = "CarrierUnavailable" };
        _shipping.ListExceptionsAsync(100, Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Ok(new PageView<ShipmentExceptionView>([recent, outage, old], 3)));

        var result = await _tools.ListOperationalExceptionsAsync(24, CancellationToken.None);

        result.Data!.Total.Should().Be(2);
        result.Data.ByType.Should().BeEquivalentTo(new Dictionary<string, int> { ["Delay"] = 1, ["CarrierUnavailable"] = 1 });
        result.Data.Truncated.Should().BeFalse();
    }

    [Fact]
    public async Task Exceptions_RejectAWindowOverAWeek() =>
        (await _tools.ListOperationalExceptionsAsync(500, CancellationToken.None)).Status.Should().Be("invalid");

    [Fact]
    public async Task AtRisk_KeepsUndeliveredShipmentsThatSlipOrHaveADelay_WorstFirst()
    {
        var promised = new DateOnly(2026, 11, 6);
        _shipping.ListShipmentsAsync(true, 100, Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(new PageView<ShipmentListItemView>(
            [OperationsData.ListItem("ORD-1", "InTransit", true, promised, promised)], 1)));
        _shipping.ListShipmentsAsync(false, 100, Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(new PageView<ShipmentListItemView>(
        [
            OperationsData.ListItem("ORD-2", "InTransit", false, promised, promised.AddDays(3)),
            OperationsData.ListItem("ORD-3", "Delivered", false, promised, promised.AddDays(5)),
            OperationsData.ListItem("ORD-4", "LabelCreated", false, promised, promised),
        ], 3)));

        var result = await _tools.ListShipmentsAtRiskAsync(CancellationToken.None);

        result.Data!.Shipments.Select(shipment => shipment.OrderNumber).Should().Equal("ORD-2", "ORD-1");
        result.Data.Scanned.Should().Be(4);
    }

    [Fact]
    public async Task WarehouseMetrics_NameTheWarehouseWithMostFailures()
    {
        _fulfillment.GetMetricsAsync(Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(new FulfillmentMetricsView(
            30,
            new Dictionary<string, long>(),
            [new WarehouseJobCountView("WH-CA1", 4, 1, 10), new WarehouseJobCountView("WH-NJ1", 2, 3, 20)])));
        _inventory.GetMetricsAsync(Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(new InventoryMetricsView(900, 8, 10)));

        var result = await _tools.GetWarehouseMetricsAsync(CancellationToken.None);

        result.Data!.MostFailures.Should().Be("WH-NJ1");
        result.Data.Warehouses[0].WarehouseCode.Should().Be("WH-NJ1");
    }

    [Fact]
    public async Task NextActions_ForAnOrderThatNeverShipped_UseTheOrder()
    {
        _orders.FindByNumberAsync("ORD-1025", Arg.Any<CancellationToken>()).Returns(ServiceReply.Ok(OperationsData.Order("Failed", "Insufficient inventory")));
        _shipping.GetShipmentByOrderAsync(OperationsData.OrderId, Arg.Any<CancellationToken>())
            .Returns(ServiceReply.Fail<ShipmentView>(CopilotToolCallOutcome.NotFound, "no shipment"));

        var result = await _tools.SuggestRecoveryActionsAsync("ORD-1025", CancellationToken.None);

        result.Data!.Actions.Should().ContainSingle().Which.Problem.Should().Be("Order failed");
    }

    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
