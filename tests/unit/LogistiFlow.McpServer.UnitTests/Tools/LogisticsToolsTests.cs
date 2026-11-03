using System.Net;
using System.Text.Json;
using LogistiFlow.McpServer.Tools;
using LogistiFlow.McpServer.UnitTests.Fixtures;
using static LogistiFlow.McpServer.UnitTests.Fixtures.McpTestServer;

namespace LogistiFlow.McpServer.UnitTests.Tools;

/// <summary>The five tools through the real MCP protocol: SDK client → server → fake gateway.</summary>
public sealed class LogisticsToolsTests : IAsyncLifetime
{
    private readonly FakeGateway _gateway = new();
    private McpTestServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await StartAsync(_gateway);

    public ValueTask DisposeAsync() => _server.DisposeAsync();

    [Fact]
    public async Task ListTools_ExposesExactlyTheFiveReadOnlyTools()
    {
        var tools = await _server.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        tools.Select(tool => tool.Name).Should().BeEquivalentTo(ToolNames.All);
        foreach (var tool in tools)
        {
            tool.Description.Should().NotBeNullOrWhiteSpace();
            tool.ProtocolTool.Annotations!.ReadOnlyHint.Should().BeTrue(tool.Name);
            tool.ProtocolTool.Annotations.DestructiveHint.Should().BeFalse(tool.Name);
            tool.ProtocolTool.OutputSchema.Should().NotBeNull($"{tool.Name} returns structured content");
        }
    }

    [Theory]
    [InlineData(ToolNames.GetOrder, "orderNumber")]
    [InlineData(ToolNames.GetShipment, "orderNumber")]
    [InlineData(ToolNames.GetInventory, "sku")]
    public async Task ListTools_DeclaresTheRequiredIdentifier(string toolName, string argument)
    {
        var tools = await _server.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var schema = JsonSerializer.SerializeToElement(tools.Single(tool => tool.Name == toolName).ProtocolTool.InputSchema);
        schema.GetProperty("required").EnumerateArray().Select(item => item.GetString()).Should().Equal(argument);
    }

    [Fact]
    public async Task GetOrder_ReturnsTheOrderAndItsHistory_ReadWithTheConfiguredToken()
    {
        _gateway.Ok("/api/orders", LogisticsData.SearchPage())
            .Ok($"/api/orders/{LogisticsData.OrderId}", LogisticsData.Order())
            .Ok($"/api/orders/{LogisticsData.OrderId}/history", LogisticsData.History());

        var result = Structured(await _server.CallAsync(ToolNames.GetOrder, new() { ["orderNumber"] = "ord-1025" }));

        result.GetProperty("order").GetProperty("orderNumber").GetString().Should().Be("ORD-1025");
        result.GetProperty("order").GetProperty("status").GetString().Should().Be("Shipped");
        result.GetProperty("history").GetArrayLength().Should().Be(4);
        _gateway.To("/api/orders").Single().Query.Should().Contain("search=ORD-1025");
        _gateway.Requests.Should().OnlyContain(request => request.Method == "GET" && request.Authorization == $"Bearer {AccessToken}");
    }

    [Theory]
    [InlineData("1025")]
    [InlineData("ORD-")]
    [InlineData("ORD-1025; DROP TABLE orders")]
    public async Task GetOrder_RejectsAMalformedNumber_WithoutCallingTheGateway(string orderNumber)
    {
        var result = await _server.CallAsync(ToolNames.GetOrder, new() { ["orderNumber"] = orderNumber });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("ORD-1025");
        _gateway.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrder_UnknownNumber_IsAToolError()
    {
        _gateway.Ok("/api/orders", new { items = Array.Empty<object>(), totalCount = 0 });

        var result = await _server.CallAsync(ToolNames.GetOrder, new() { ["orderNumber"] = "ORD-9999" });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("Order ORD-9999 does not exist.");
    }

    [Fact]
    public async Task GetOrder_UserTheGatewayForbids_IsAToolError_NotData()
    {
        _gateway.Fail("/api/orders", HttpStatusCode.Forbidden, "Forbidden.");

        var result = await _server.CallAsync(ToolNames.GetOrder, new() { ["orderNumber"] = "ORD-1025" });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("not allowed");
    }

    [Fact]
    public async Task GetInventory_ReturnsStockPerWarehouse()
    {
        _gateway.Ok("/api/inventory/availability/SKU-1003", LogisticsData.Availability());

        var result = Structured(await _server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "SKU-1003" }));

        result.GetProperty("totalAvailable").GetInt32().Should().Be(42);
        result.GetProperty("warehouses")[0].GetProperty("reserved").GetInt32().Should().Be(8);
    }

    [Fact]
    public async Task GetInventory_RejectsASkuWithPathCharacters()
    {
        var result = await _server.CallAsync(ToolNames.GetInventory, new() { ["sku"] = "../metrics/inventory" });

        result.IsError.Should().BeTrue();
        _gateway.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetShipment_ReturnsTrackingAndOpenExceptions()
    {
        _gateway.Ok("/api/orders", LogisticsData.SearchPage())
            .Ok($"/api/orders/{LogisticsData.OrderId}", LogisticsData.Order())
            .Ok($"/api/shipping/shipments/by-order/{LogisticsData.OrderId}", LogisticsData.Shipment());

        var result = Structured(await _server.CallAsync(ToolNames.GetShipment, new() { ["orderNumber"] = "ORD-1025" }));

        result.GetProperty("trackingNumber").GetString().Should().Be("SIMX-0001");
        result.GetProperty("isDelayed").GetBoolean().Should().BeTrue();
        result.GetProperty("exceptions")[0].GetProperty("details").GetString().Should().Be("Severe weather on the route.");
    }

    [Fact]
    public async Task GetShipment_OrderNotShippedYet_SaysSoWithTheOrderStatus()
    {
        _gateway.Ok("/api/orders", LogisticsData.SearchPage())
            .Ok($"/api/orders/{LogisticsData.OrderId}", LogisticsData.Order(status: "Picking"));

        var result = await _server.CallAsync(ToolNames.GetShipment, new() { ["orderNumber"] = "ORD-1025" });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain("Order ORD-1025 has no shipment yet; its status is Picking.");
    }

    [Fact]
    public async Task ListExceptions_PassesNormalisedFiltersAndTheLimit()
    {
        _gateway.Ok("/api/shipping/exceptions", LogisticsData.ExceptionPage());

        var result = Structured(await _server.CallAsync(ToolNames.ListExceptions, new() { ["status"] = "open", ["type"] = "delay", ["limit"] = 5 }));

        result.GetProperty("totalCount").GetInt64().Should().Be(7);
        result.GetProperty("status").GetString().Should().Be("Open");
        result.GetProperty("items").GetArrayLength().Should().Be(1);
        _gateway.To("/api/shipping/exceptions").Single().Query.Should().Be("?pageSize=5&status=Open&type=Delay");
    }

    [Fact]
    public async Task ListExceptions_WithoutArguments_UsesTheDefaultLimitAndNoFilters()
    {
        _gateway.Ok("/api/shipping/exceptions", LogisticsData.ExceptionPage());

        Structured(await _server.CallAsync(ToolNames.ListExceptions));

        _gateway.To("/api/shipping/exceptions").Single().Query.Should().Be("?pageSize=20");
    }

    [Theory]
    [InlineData("type", "Lost")]
    [InlineData("status", "Closed")]
    [InlineData("limit", 0)]
    [InlineData("limit", 101)]
    public async Task ListExceptions_RejectsAnUnknownFilterOrLimit(string argument, object value)
    {
        var result = await _server.CallAsync(ToolNames.ListExceptions, new() { [argument] = value });

        result.IsError.Should().BeTrue();
        Text(result).Should().Contain(argument);
        _gateway.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWarehouseMetrics_CombinesFulfillmentAndInventory()
    {
        _gateway.Ok("/api/metrics/fulfillment", LogisticsData.FulfillmentMetrics())
            .Ok("/api/metrics/inventory", LogisticsData.InventoryMetrics());

        var result = Structured(await _server.CallAsync(ToolNames.GetWarehouseMetrics));

        result.GetProperty("totalJobs").GetInt64().Should().Be(12);
        result.GetProperty("warehouses").EnumerateArray().Select(warehouse => warehouse.GetProperty("failed").GetInt64()).Should().Equal(1, 1);
        result.GetProperty("jobsByStatus").GetProperty("Failed").GetInt64().Should().Be(2);
        result.GetProperty("inventory").GetProperty("lowStockPositions").GetInt64().Should().Be(3);
    }

    [Fact]
    public async Task GetWarehouseMetrics_OneSourceUnavailable_IsAToolError_NotPartialData()
    {
        _gateway.Ok("/api/metrics/fulfillment", LogisticsData.FulfillmentMetrics())
            .Fail("/api/metrics/inventory", HttpStatusCode.Forbidden, "Forbidden.");

        var result = await _server.CallAsync(ToolNames.GetWarehouseMetrics);

        result.IsError.Should().BeTrue();
    }
}
