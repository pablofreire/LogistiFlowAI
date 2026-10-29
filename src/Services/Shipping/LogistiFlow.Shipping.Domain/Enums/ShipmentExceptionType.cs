namespace LogistiFlow.Shipping.Domain.Enums;

/// <summary>Operational exceptions an operator should see. At most one of each type is open per shipment.</summary>
public enum ShipmentExceptionType
{
    /// <summary>The carrier booking API answered with an error or did not answer in time.</summary>
    CarrierUnavailable = 0,

    /// <summary>The delivery estimate moved later, or is after the promised date.</summary>
    Delay = 1,

    DeliveryAttemptFailed = 2,

    /// <summary>The carrier gave up and is returning the package.</summary>
    ReturnedToSender = 3,
}
