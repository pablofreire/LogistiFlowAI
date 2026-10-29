using LogistiFlow.Contracts.IntegrationEvents.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Models;

namespace LogistiFlow.Contracts.IntegrationEvents.Fulfillment;

/// <summary>The package left the warehouse dock. Inventory consumes the reservation; Shipping books a carrier.</summary>
public sealed record FulfillmentShippedEvent(
    Guid FulfillmentJobId,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    Guid ReservationId,
    IReadOnlyList<OrderLineItem> Lines,
    DateTimeOffset ShippedAt) : IIntegrationEvent
{
    public static string EventType => "fulfillment.fulfillment-shipped";

    public static int Version => 1;
}
