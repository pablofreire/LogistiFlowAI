using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.UnitTests.Domain.Entities;

public sealed class ShipmentEntityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Estimate = new(2026, 11, 4);

    [Fact]
    public void CreatePendingBooking_AssignsTheCarrierAndWaitsForBooking()
    {
        var shipment = Shipment(promised: null, country: "US");

        shipment.Status.Should().Be(ShipmentStatus.PendingBooking);
        shipment.CarrierCode.Should().Be("RDRN");
        shipment.Exceptions.Should().BeEmpty();
    }

    [Fact]
    public void BookingFailures_OpenOneCarrierExceptionAndGiveUpAtTheLimit()
    {
        var shipment = Shipment();

        shipment.RecordBookingFailure("HTTP 503", attempt: 1, maxAttempts: 3, Now);
        shipment.RecordBookingFailure("HTTP 503", attempt: 2, maxAttempts: 3, Now);
        shipment.Status.Should().Be(ShipmentStatus.PendingBooking);

        shipment.RecordBookingFailure("timeout", attempt: 3, maxAttempts: 3, Now);

        shipment.Status.Should().Be(ShipmentStatus.BookingFailed);
        shipment.Exceptions.Should().ContainSingle(e => e.Type == ShipmentExceptionType.CarrierUnavailable && e.IsOpen)
            .Which.Details.Should().Contain("attempt 3 of 3");
    }

    [Fact]
    public void MarkBooked_ResolvesTheCarrierException()
    {
        var shipment = Shipment();
        shipment.RecordBookingFailure("HTTP 503", 1, 6, Now);

        shipment.MarkBooked("RDRN10000001", Estimate, Now).IsSuccess.Should().BeTrue();

        shipment.Status.Should().Be(ShipmentStatus.LabelCreated);
        shipment.TrackingNumber.Should().Be("RDRN10000001");
        shipment.Exceptions.Should().ContainSingle().Which.Status.Should().Be(ShipmentExceptionStatus.Resolved);
    }

    [Fact]
    public void MarkBooked_EstimateAfterThePromisedDate_IsADelayFromDayOne()
    {
        var shipment = Shipment(promised: new DateOnly(2026, 11, 2));

        shipment.MarkBooked("RDRN10000001", Estimate, Now);

        shipment.IsDelayed.Should().BeTrue();
        shipment.Exceptions.Should().ContainSingle(e => e.Type == ShipmentExceptionType.Delay).Which.Details.Should().Contain("2026-11-02");
    }

    [Fact]
    public void NormalTrackingSequence_EndsDeliveredWithoutExceptions()
    {
        var shipment = Booked();

        Apply(shipment, "PICKED_UP", "IN_TRANSIT", "OUT_FOR_DELIVERY", "DELIVERED");

        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.DeliveredAt.Should().NotBeNull();
        shipment.Exceptions.Should().BeEmpty();
    }

    [Fact]
    public void DelayedEvent_OpensADelayThatDeliveryResolves()
    {
        var shipment = Booked();

        shipment.ApplyTrackingEvent(Event("DELAYED", Estimate.AddDays(2)), Now);

        shipment.IsDelayed.Should().BeTrue();
        shipment.EstimatedDelivery.Should().Be(Estimate.AddDays(2));
        shipment.Exceptions.Single().Details.Should().Contain("was 2026-11-04");

        Apply(shipment, "OUT_FOR_DELIVERY", "DELIVERED");

        shipment.IsDelayed.Should().BeFalse();
        shipment.Exceptions.Single().Resolution.Should().StartWith("Delivered on");
    }

    [Fact]
    public void FailedAttemptThenRedelivery_EndsDeliveredWithTheExceptionResolved()
    {
        var shipment = Booked();

        Apply(shipment, "IN_TRANSIT", "OUT_FOR_DELIVERY", "DELIVERY_ATTEMPT_FAILED");
        shipment.Status.Should().Be(ShipmentStatus.DeliveryAttemptFailed);
        shipment.Exceptions.Should().ContainSingle(e => e.Type == ShipmentExceptionType.DeliveryAttemptFailed && e.IsOpen);

        Apply(shipment, "OUT_FOR_DELIVERY", "DELIVERED");

        shipment.Status.Should().Be(ShipmentStatus.Delivered);
        shipment.Exceptions.Should().OnlyContain(e => !e.IsOpen);
    }

    [Fact]
    public void ReturnedToSender_IsTerminalAndLeavesAnOpenException()
    {
        var shipment = Booked();

        Apply(shipment, "OUT_FOR_DELIVERY", "DELIVERY_ATTEMPT_FAILED", "RETURNED_TO_SENDER");

        shipment.Status.Should().Be(ShipmentStatus.ReturnedToSender);
        shipment.Exceptions.Should().ContainSingle(e => e.IsOpen).Which.Type.Should().Be(ShipmentExceptionType.ReturnedToSender);
        shipment.ApplyTrackingEvent(Event("DELIVERED"), Now).Should().BeFalse();
        shipment.Status.Should().Be(ShipmentStatus.ReturnedToSender);
    }

    [Fact]
    public void BackwardsEvent_IsKeptOutOfTheStatus()
    {
        var shipment = Booked();
        Apply(shipment, "OUT_FOR_DELIVERY");

        shipment.ApplyTrackingEvent(Event("PICKED_UP"), Now).Should().BeFalse();

        shipment.Status.Should().Be(ShipmentStatus.OutForDelivery);
    }

    [Fact]
    public void ResolveException_ByAnOperator_RecordsWhoAndWhy()
    {
        var shipment = Booked();
        Apply(shipment, "OUT_FOR_DELIVERY", "DELIVERY_ATTEMPT_FAILED", "RETURNED_TO_SENDER");
        var open = shipment.Exceptions.Single(e => e.IsOpen);

        shipment.ResolveException(open.PublicId, "Customer confirmed a new address", "ops@example.com", Now).IsSuccess.Should().BeTrue();
        shipment.ResolveException(open.PublicId, "again", "ops@example.com", Now).Error.Code.Should().Be("shipping.exception_already_resolved");

        open.ResolvedBy.Should().Be("ops@example.com");
        open.IsModified.Should().BeTrue();
    }

    [Fact]
    public void RetryBooking_OnlyFromBookingFailed()
    {
        var shipment = Shipment();
        shipment.RetryBooking(Now).Error.Code.Should().Be("shipping.invalid_transition");

        shipment.RecordBookingFailure("down", 1, 1, Now);
        shipment.RetryBooking(Now).IsSuccess.Should().BeTrue();

        shipment.Status.Should().Be(ShipmentStatus.PendingBooking);
    }

    private static ShipmentEntity Shipment(DateOnly? promised = null, string country = "US") =>
        ShipmentEntity.CreatePendingBooking(
            new OrderDestination(Guid.NewGuid(), "ORD-1001", "Acme", "Austin", country, promised),
            Guid.NewGuid(),
            "WH-TX1",
            Now,
            Now);

    private static ShipmentEntity Booked()
    {
        var shipment = Shipment();
        shipment.MarkBooked("RDRN10000001", Estimate, Now);
        return shipment;
    }

    private static void Apply(ShipmentEntity shipment, params string[] codes)
    {
        foreach (var code in codes)
        {
            shipment.ApplyTrackingEvent(Event(code), Now);
        }
    }

    private static CarrierTrackingEvent Event(string code, DateOnly? estimate = null) =>
        new(Guid.NewGuid().ToString(), code, code.Replace('_', ' '), "Austin, US", Now, estimate ?? Estimate);
}
