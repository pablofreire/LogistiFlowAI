using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.Contracts.IntegrationEvents.Fulfillment;

public sealed record FulfillmentPackedEvent(
    Guid FulfillmentJobId,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    DateTimeOffset PackedAt) : IIntegrationEvent
{
    public static string EventType => "fulfillment.fulfillment-packed";

    public static int Version => 1;
}
