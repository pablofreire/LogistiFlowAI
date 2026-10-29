using LogistiFlow.Contracts.IntegrationEvents.Interfaces;
using LogistiFlow.Contracts.IntegrationEvents.Models;

namespace LogistiFlow.Contracts.IntegrationEvents.Inventory;

public sealed record InventoryReservationFailedEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason,
    IReadOnlyList<InventoryShortage> Shortages,
    DateTimeOffset FailedAt) : IIntegrationEvent
{
    public static string EventType => "inventory.inventory-reservation-failed";

    public static int Version => 1;
}
