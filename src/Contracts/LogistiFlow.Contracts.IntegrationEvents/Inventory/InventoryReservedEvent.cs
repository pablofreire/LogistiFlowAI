using LogistiFlow.Contracts.IntegrationEvents.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Models;

namespace LogistiFlow.Contracts.IntegrationEvents.Inventory;

public sealed record InventoryReservedEvent(
    Guid ReservationId,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    IReadOnlyList<OrderLineItem> Lines,
    DateTimeOffset ReservedAt) : IIntegrationEvent
{
    public static string EventType => "inventory.inventory-reserved";

    public static int Version => 1;
}
