using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Implementations;
using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;
using LogistiFlow.Inventory.Domain.ReadModels;
using LogistiFlow.Inventory.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LogistiFlow.Inventory.UnitTests.Application.UseCases.Reservations.Reserve.Implementations;

public sealed class ReserveInventoryUseCaseTests
{
    private readonly IStockRepository _stock = Substitute.For<IStockRepository>();
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly IIntegrationEventOutbox _outbox = Substitute.For<IIntegrationEventOutbox>();
    private readonly ReserveInventoryUseCase _useCase;

    public ReserveInventoryUseCaseTests()
    {
        _useCase = new ReserveInventoryUseCase(
            _stock,
            _reservations,
            _outbox,
            new PassThroughUnitOfWork(),
            new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            NullLogger<ReserveInventoryUseCase>.Instance);
        _reservations.TryAddAsync(Arg.Any<ReservationEntity>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task ExecuteAsync_FirstWarehouseHasStock_ReservesEveryLineThereAndPublishesReserved()
    {
        Candidates(Warehouse("WH-CA1", (10, "SKU-A"), (11, "SKU-B")), Warehouse("WH-NJ1", (20, "SKU-A"), (21, "SKU-B")));
        _stock.TryReserveAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _useCase.ExecuteAsync(Request(("SKU-A", 2), ("SKU-B", 1)), CancellationToken.None);

        result.Value.Status.Should().Be(ReservationStatus.Reserved);
        result.Value.WarehouseCode.Should().Be("WH-CA1");
        await _stock.Received(1).TryReserveAsync(10, 2, Arg.Any<CancellationToken>());
        await _stock.Received(1).TryReserveAsync(11, 1, Arg.Any<CancellationToken>());
        await _stock.DidNotReceive().TryReserveAsync(20, Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(Arg.Is<InventoryReservedEvent>(e => e.WarehouseCode == "WH-CA1" && e.Lines.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_LostRaceOnSecondLine_UndoesFirstLineAndUsesNextWarehouse()
    {
        Candidates(Warehouse("WH-CA1", (10, "SKU-A"), (11, "SKU-B")), Warehouse("WH-NJ1", (20, "SKU-A"), (21, "SKU-B")));
        _stock.TryReserveAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _stock.TryReserveAsync(11, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _useCase.ExecuteAsync(Request(("SKU-A", 2), ("SKU-B", 1)), CancellationToken.None);

        result.Value.WarehouseCode.Should().Be("WH-NJ1");
        await _stock.Received(1).ReleaseAsync(10, 2, Arg.Any<CancellationToken>());
        await _stock.Received(2).RecordMovementAsync(Arg.Any<long>(), StockMovementType.Reservation, Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_NoWarehouseHasStock_PublishesFailureWithShortages()
    {
        Candidates();
        _stock.GetAvailabilityAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([new SkuAvailability("SKU-OOS", 0, 0)]);

        var result = await _useCase.ExecuteAsync(Request(("SKU-OOS", 3)), CancellationToken.None);

        result.Value.Status.Should().Be(ReservationStatus.Failed);
        result.Value.FailureReason.Should().Be("Insufficient inventory");
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<InventoryReservationFailedEvent>(e => e.Shortages.Count == 1 && e.Shortages[0].Requested == 3 && e.Shortages[0].Available == 0),
            Arg.Any<CancellationToken>());
        await _stock.DidNotReceiveWithAnyArgs().TryReserveAsync(default, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_StockExistsOnlySplitAcrossWarehouses_FailsWithSplitReason()
    {
        Candidates();
        _stock.GetAvailabilityAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([new SkuAvailability("SKU-A", 5, 9), new SkuAvailability("SKU-B", 5, 5)]);

        var result = await _useCase.ExecuteAsync(Request(("SKU-A", 5), ("SKU-B", 5)), CancellationToken.None);

        result.Value.FailureReason.Should().Be("No single warehouse holds every line");
    }

    [Fact]
    public async Task ExecuteAsync_ReservationAlreadyExists_ChangesNothing()
    {
        var request = Request(("SKU-A", 1));
        _reservations.GetByOrderIdAsync(request.OrderId, Arg.Any<CancellationToken>())
            .Returns(ReservationEntity.CancelledBeforeReservation(request.OrderId, request.OrderNumber, DateTimeOffset.UnixEpoch));

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.Value.AlreadyProcessed.Should().BeTrue();
        result.Value.Status.Should().Be(ReservationStatus.Cancelled);
        await _stock.DidNotReceiveWithAnyArgs().FindCandidateWarehousesAsync(default!, default);
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync<InventoryReservedEvent>(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_SameSkuOnTwoLines_ReservesTheSum()
    {
        Candidates(Warehouse("WH-NJ1", (10, "SKU-A")));
        _stock.TryReserveAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        await _useCase.ExecuteAsync(Request(("SKU-A", 2), ("sku-a", 3)), CancellationToken.None);

        await _stock.Received(1).TryReserveAsync(10, 5, Arg.Any<CancellationToken>());
    }

    private void Candidates(params WarehouseStockCandidate[] candidates) =>
        _stock.FindCandidateWarehousesAsync(Arg.Any<IReadOnlyDictionary<string, int>>(), Arg.Any<CancellationToken>()).Returns(candidates);

    private static WarehouseStockCandidate Warehouse(string code, params (long PositionId, string Sku)[] positions) =>
        new(code, positions.Select(position => new CandidateStockPosition(position.PositionId, position.Sku, 100)).ToList());

    private static ReserveInventoryRequest Request(params (string Sku, int Quantity)[] lines) =>
        new(Guid.NewGuid(), "ORD-1001", lines.Select(line => new OrderLineItem(line.Sku, line.Quantity)).ToList());
}
