using System.Globalization;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.IntegrationEventHandlers;

public sealed class InventoryReservationFailedEventHandler(OrderStatusEventApplier applier)
    : IIntegrationEventHandler<InventoryReservationFailedEvent>
{
    public Task HandleAsync(InventoryReservationFailedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return applier.ApplyAsync(
            integrationEvent.OrderId,
            OrderStatus.Failed,
            DescribeFailure(integrationEvent),
            InventoryReservationFailedEvent.EventType,
            cancellationToken);
    }

    /// <summary>"Insufficient inventory: SKU-OOS-001 requested 2, available 0".</summary>
    public static string DescribeFailure(InventoryReservationFailedEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        if (integrationEvent.Shortages.Count == 0)
        {
            return integrationEvent.Reason;
        }

        var shortages = integrationEvent.Shortages.Select(shortage => string.Create(
            CultureInfo.InvariantCulture,
            $"{shortage.Sku} requested {shortage.Requested}, available {shortage.Available}"));
        var description = $"{integrationEvent.Reason}: {string.Join("; ", shortages)}";
        return description.Length <= 500 ? description : description[..500];
    }
}
