using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Shipping;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Models;
using LogistiFlow.Shipping.Application.Options;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Implementations;
using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;
using NSubstitute;

namespace LogistiFlow.Shipping.UnitTests.Application.UseCases.Shipments.PollTracking.Implementations;

public sealed class PollShipmentTrackingUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly IShipmentWorkQueue _workQueue = Substitute.For<IShipmentWorkQueue>();
    private readonly ICarrierGateway _carrier = Substitute.For<ICarrierGateway>();
    private readonly IIntegrationEventOutbox _outbox = Substitute.For<IIntegrationEventOutbox>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly HashSet<string> _storedEventIds = [];
    private readonly PollShipmentTrackingUseCase _useCase;

    public PollShipmentTrackingUseCaseTests()
    {
        _unitOfWork.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<PollShipmentTrackingResponse>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<Result<PollShipmentTrackingResponse>>>>()(CancellationToken.None));
        _shipments.TryUpdateAsync(Arg.Any<ShipmentEntity>(), Arg.Any<ShipmentSchedule>(), Arg.Any<CancellationToken>()).Returns(true);
        _shipments.TryAddTrackingEventAsync(Arg.Any<long>(), Arg.Any<CarrierTrackingEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => _storedEventIds.Add(callInfo.Arg<CarrierTrackingEvent>().CarrierEventId));
        _useCase = new PollShipmentTrackingUseCase(
            _shipments,
            _workQueue,
            _carrier,
            _outbox,
            _unitOfWork,
            Microsoft.Extensions.Options.Options.Create(new ShippingWorkerOptions()),
            TimeProvider.System);
    }

    [Fact]
    public async Task Delivery_IsPublishedOnce_AndARepeatedPollAddsNothing()
    {
        var shipment = Booked();
        CarrierReturns("LABEL_CREATED", "PICKED_UP", "IN_TRANSIT", "OUT_FOR_DELIVERY", "DELIVERED");

        var first = await _useCase.ExecuteAsync(new PollShipmentTrackingRequest(shipment.PublicId), CancellationToken.None);
        var second = await _useCase.ExecuteAsync(new PollShipmentTrackingRequest(shipment.PublicId), CancellationToken.None);

        first.Value.NewEvents.Should().Be(5);
        first.Value.Status.Should().Be(ShipmentStatus.Delivered);
        second.Value.NewEvents.Should().Be(0);
        await _outbox.Received(1).EnqueueAsync(Arg.Is<ShipmentDeliveredEvent>(e => e.OrderId == shipment.OrderId), Arg.Any<CancellationToken>());
        await _shipments.Received(1).TryUpdateAsync(shipment, ShipmentSchedule.None, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnToSender_PublishesTheReturn()
    {
        var shipment = Booked();
        CarrierReturns("LABEL_CREATED", "OUT_FOR_DELIVERY", "DELIVERY_ATTEMPT_FAILED", "RETURNED_TO_SENDER");

        await _useCase.ExecuteAsync(new PollShipmentTrackingRequest(shipment.PublicId), CancellationToken.None);

        await _outbox.Received(1).EnqueueAsync(Arg.Any<ShipmentReturnedEvent>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueAsync<ShipmentDeliveredEvent>(default!, default);
    }

    [Fact]
    public async Task CarrierError_ReschedulesWithoutTouchingTheShipment()
    {
        var shipment = Booked();
        _carrier.GetTrackingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(CarrierTrackingResult.Failed("HTTP 404 Not Found"));

        var result = await _useCase.ExecuteAsync(new PollShipmentTrackingRequest(shipment.PublicId), CancellationToken.None);

        result.Value.CarrierError.Should().Be("HTTP 404 Not Found");
        await _workQueue.Received(1).RescheduleAsync(shipment.PublicId, Arg.Is<ShipmentSchedule>(s => s.NextPollIn != null), Arg.Any<CancellationToken>());
        await _shipments.DidNotReceiveWithAnyArgs().TryUpdateAsync(default!, default!, default);
    }

    private ShipmentEntity Booked()
    {
        var shipment = ShipmentEntity.CreatePendingBooking(
            new OrderDestination(Guid.NewGuid(), "ORD-1001", "Acme", "Austin", "US", null),
            Guid.NewGuid(),
            "WH-TX1",
            Now,
            Now);
        shipment.MarkBooked("RDRN10000001", new DateOnly(2026, 11, 4), Now);
        _shipments.GetByPublicIdAsync(shipment.PublicId, Arg.Any<CancellationToken>()).Returns(shipment);
        return shipment;
    }

    private void CarrierReturns(params string[] codes)
    {
        var events = codes.Select((code, index) => new CarrierTrackingEvent($"RDRN10000001-{index:D2}", code, code, "Austin, US", Now.AddMinutes(index), new DateOnly(2026, 11, 4))).ToList();
        _carrier.GetTrackingAsync("RDRN", "RDRN10000001", Arg.Any<CancellationToken>()).Returns(CarrierTrackingResult.Read(events));
    }
}
