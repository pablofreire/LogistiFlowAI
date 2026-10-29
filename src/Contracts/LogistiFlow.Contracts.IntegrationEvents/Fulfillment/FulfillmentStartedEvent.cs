using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.Contracts.IntegrationEvents.Fulfillment;

public sealed record FulfillmentStartedEvent(
    Guid FulfillmentJobId,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    DateTimeOffset StartedAt) : IIntegrationEvent
{
    public static string EventType => "fulfillment.fulfillment-started";

    public static int Version => 1;
}
