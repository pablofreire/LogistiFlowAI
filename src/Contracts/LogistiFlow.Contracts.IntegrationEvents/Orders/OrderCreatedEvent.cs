using LogistiFlow.Contracts.IntegrationEvents.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Models;

namespace LogistiFlow.Contracts.IntegrationEvents.Orders;

/// <summary>
/// An order was accepted. The destination fields were added in Phase 2 for the Shipping service; they are nullable
/// so that messages written before them still deserialize (an additive change within version 1).
/// </summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<OrderLineItem> Lines,
    DateTimeOffset CreatedAt,
    string? CustomerName = null,
    string? DestinationCity = null,
    string? DestinationCountry = null,
    DateOnly? PromisedDeliveryDate = null) : IIntegrationEvent
{
    public static string EventType => "orders.order-created";

    public static int Version => 1;
}
