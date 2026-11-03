using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Implementations;
using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;
using LogistiFlow.Inventory.UnitTests.Fakes;
using NSubstitute;

namespace LogistiFlow.Inventory.UnitTests.Application.UseCases.Reservations.Release.Implementations;

public sealed class ReleaseInventoryUseCaseTests
{
    private readonly IStockRepository _stock = Substitute.For<IStockRepository>();
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly IIntegrationEventOutbox _outbox = Substitute.For<IIntegrationEventOutbox>();
    private readonly ReleaseInventoryUseCase _useCase;

    public ReleaseInventoryUseCaseTests()
    {
        _useCase = new ReleaseInventoryUseCase(_stock, _reservations, _outbox, new PassThroughUnitOfWork(), TimeProvider.System);
    }

    [Fact]
    public async Task ExecuteAsync_ReservedOrder_ReturnsEveryLineAndPublishesReleased()
    {
        var orderId = Guid.NewGuid();
        var reservation = ReservationEntity.Reserved(orderId, "ORD-1", "WH-NJ1", [new(10, "SKU-A", 2), new(11, "SKU-B", 3)], DateTimeOffset.UnixEpoch);
        _reservations.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _useCase.ExecuteAsync(new ReleaseInventoryRequest(orderId, "ORD-1"), CancellationToken.None);

        result.Value.Should().Be(new ReleaseInventoryResponse(ReservationStatus.Released, 5));
        await _stock.Received(1).ReleaseAsync(10, 2, Arg.Any<CancellationToken>());
        await _stock.Received(1).ReleaseAsync(11, 3, Arg.Any<CancellationToken>());
        await _reservations.Received(1).UpdateStatusAsync(reservation, Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(Arg.Any<InventoryReleasedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CancellationBeforeReservation_WritesTombstone()
    {
        var orderId = Guid.NewGuid();

        var result = await _useCase.ExecuteAsync(new ReleaseInventoryRequest(orderId, "ORD-2"), CancellationToken.None);

        result.Value.Status.Should().Be(ReservationStatus.Cancelled);
        await _reservations.Received(1).TryAddAsync(
            Arg.Is<ReservationEntity>(r => r.OrderId == orderId && r.Status == ReservationStatus.Cancelled),
            Arg.Any<CancellationToken>());
        await _stock.DidNotReceiveWithAnyArgs().ReleaseAsync(default, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_FailedReservation_HasNothingToRelease()
    {
        var orderId = Guid.NewGuid();
        _reservations.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(ReservationEntity.Failed(orderId, "ORD-3", "Insufficient inventory", DateTimeOffset.UnixEpoch));

        var result = await _useCase.ExecuteAsync(new ReleaseInventoryRequest(orderId, "ORD-3"), CancellationToken.None);

        result.Value.Should().Be(new ReleaseInventoryResponse(ReservationStatus.Failed, 0));
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync<InventoryReleasedEvent>(default!, default);
    }
}
