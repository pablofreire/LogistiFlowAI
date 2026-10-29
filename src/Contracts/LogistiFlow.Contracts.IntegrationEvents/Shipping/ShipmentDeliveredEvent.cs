using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.Contracts.IntegrationEvents.Shipping;

public sealed record ShipmentDeliveredEvent(
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    string CarrierCode,
    string TrackingNumber,
    DateTimeOffset DeliveredAt) : IIntegrationEvent
{
    public static string EventType => "shipping.shipment-delivered";

    public static int Version => 1;
}
