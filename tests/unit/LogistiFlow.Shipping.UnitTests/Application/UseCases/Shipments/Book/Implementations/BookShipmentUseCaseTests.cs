using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Models;
using LogistiFlow.Shipping.Application.Options;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Implementations;
using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;
using NSubstitute;

namespace LogistiFlow.Shipping.UnitTests.Application.UseCases.Shipments.Book.Implementations;

public sealed class BookShipmentUseCaseTests
{
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly ICarrierGateway _carrier = Substitute.For<ICarrierGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ShippingWorkerOptions _options = new() { BookingMaxAttempts = 3, BookingBaseDelay = TimeSpan.FromSeconds(2), TrackingPollInterval = TimeSpan.FromSeconds(5) };
    private readonly BookShipmentUseCase _useCase;
    private ShipmentSchedule? _savedSchedule;

    public BookShipmentUseCaseTests()
    {
        _unitOfWork.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<BookShipmentResponse>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<Result<BookShipmentResponse>>>>()(CancellationToken.None));
        _shipments.TryUpdateAsync(Arg.Any<ShipmentEntity>(), Arg.Do<ShipmentSchedule>(schedule => _savedSchedule = schedule), Arg.Any<CancellationToken>())
            .Returns(true);
        _useCase = new BookShipmentUseCase(_shipments, _carrier, _unitOfWork, Microsoft.Extensions.Options.Options.Create(_options), TimeProvider.System);
    }

    [Fact]
    public async Task Success_BooksAndSchedulesTheFirstPoll_WithTheShipmentIdAsIdempotencyKey()
    {
        var shipment = Pending();
        _carrier.BookAsync(Arg.Any<CarrierBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(CarrierBookingResult.Booked("RDRN10000001", new DateOnly(2026, 11, 4)));

        var result = await _useCase.ExecuteAsync(new BookShipmentRequest(shipment.PublicId, 1), CancellationToken.None);

        result.Value.Booked.Should().BeTrue();
        result.Value.Status.Should().Be(ShipmentStatus.LabelCreated);
        _savedSchedule.Should().Be(ShipmentSchedule.PollIn(TimeSpan.FromSeconds(5)));
        await _carrier.Received(1).BookAsync(
            Arg.Is<CarrierBookingRequest>(request => request.IdempotencyKey == shipment.PublicId.ToString() && request.CarrierCode == "RDRN"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    public async Task Failure_RetriesWithExponentialBackoff(int attempt, int expectedSeconds)
    {
        var shipment = Pending();
        _carrier.BookAsync(Arg.Any<CarrierBookingRequest>(), Arg.Any<CancellationToken>()).Returns(CarrierBookingResult.Failed("HTTP 503"));

        var result = await _useCase.ExecuteAsync(new BookShipmentRequest(shipment.PublicId, attempt), CancellationToken.None);

        result.Value.Booked.Should().BeFalse();
        result.Value.CarrierError.Should().Be("HTTP 503");
        _savedSchedule.Should().Be(ShipmentSchedule.BookIn(TimeSpan.FromSeconds(expectedSeconds)));
        shipment.Exceptions.Should().ContainSingle(e => e.Type == ShipmentExceptionType.CarrierUnavailable && e.IsOpen);
    }

    [Fact]
    public async Task FailureAtTheLastAttempt_StopsScheduling()
    {
        var shipment = Pending();
        _carrier.BookAsync(Arg.Any<CarrierBookingRequest>(), Arg.Any<CancellationToken>()).Returns(CarrierBookingResult.Failed("timeout"));

        var result = await _useCase.ExecuteAsync(new BookShipmentRequest(shipment.PublicId, 3), CancellationToken.None);

        result.Value.Status.Should().Be(ShipmentStatus.BookingFailed);
        _savedSchedule.Should().Be(ShipmentSchedule.None);
    }

    [Fact]
    public async Task ShipmentNoLongerPending_DoesNotCallTheCarrier()
    {
        var shipment = Pending();
        shipment.MarkBooked("RDRN10000001", new DateOnly(2026, 11, 4), DateTimeOffset.UtcNow);

        var result = await _useCase.ExecuteAsync(new BookShipmentRequest(shipment.PublicId, 2), CancellationToken.None);

        result.Value.Booked.Should().BeFalse();
        await _carrier.DidNotReceiveWithAnyArgs().BookAsync(default!, default);
    }

    private ShipmentEntity Pending()
    {
        var shipment = ShipmentEntity.CreatePendingBooking(
            new OrderDestination(Guid.NewGuid(), "ORD-1001", "Acme", "Austin", "US", null),
            Guid.NewGuid(),
            "WH-TX1",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        _shipments.GetByPublicIdAsync(shipment.PublicId, Arg.Any<CancellationToken>()).Returns(shipment);
        return shipment;
    }
}
