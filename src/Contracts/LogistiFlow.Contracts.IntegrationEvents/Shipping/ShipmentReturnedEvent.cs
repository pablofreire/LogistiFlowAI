using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.Contracts.IntegrationEvents.Shipping;

/// <summary>The carrier gave up delivering and is returning the package to the warehouse.</summary>
public sealed record ShipmentReturnedEvent(
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    string CarrierCode,
    string TrackingNumber,
    string Reason,
    DateTimeOffset ReturnedAt) : IIntegrationEvent
{
    public static string EventType => "shipping.shipment-returned";

    public static int Version => 1;
}
