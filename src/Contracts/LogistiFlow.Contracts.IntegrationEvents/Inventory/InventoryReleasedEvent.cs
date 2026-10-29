using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.Contracts.IntegrationEvents.Inventory;

public sealed record InventoryReleasedEvent(Guid ReservationId, Guid OrderId, string OrderNumber, DateTimeOffset ReleasedAt) : IIntegrationEvent
{
    public static string EventType => "inventory.inventory-released";

    public static int Version => 1;
}
