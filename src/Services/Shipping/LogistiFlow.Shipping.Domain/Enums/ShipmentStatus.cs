namespace LogistiFlow.Shipping.Domain.Enums;

/// <summary>Shipment lifecycle. Allowed transitions are in <see cref="Services.ShipmentStateMachine"/>.</summary>
public enum ShipmentStatus
{
    /// <summary>Waiting for the booking worker to book the carrier.</summary>
    PendingBooking = 0,

    /// <summary>The carrier kept refusing or timing out; an ops manager can retry the booking.</summary>
    BookingFailed = 1,

    /// <summary>Booked: the carrier issued a tracking number and an estimate.</summary>
    LabelCreated = 2,

    InTransit = 3,
    OutForDelivery = 4,

    /// <summary>A delivery attempt failed; the carrier will try again or return the package.</summary>
    DeliveryAttemptFailed = 5,

    Delivered = 6,
    ReturnedToSender = 7,
}
