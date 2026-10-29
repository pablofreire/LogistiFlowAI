using LogistiFlow.Contracts.IntegrationEvents.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Models;

namespace LogistiFlow.Contracts.IntegrationEvents.Orders;

public sealed record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<OrderLineItem> Lines,
    DateTimeOffset CreatedAt) : IIntegrationEvent
{
    public static string EventType => "orders.order-created";

    public static int Version => 1;
}
