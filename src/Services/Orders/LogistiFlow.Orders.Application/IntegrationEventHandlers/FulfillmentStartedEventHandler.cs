using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.IntegrationEventHandlers;

/// <summary>
/// Moves the order to <c>Picking</c>. <c>FulfillmentStarted</c> and <c>InventoryReserved</c> come from different
/// services, so this event can overtake the reservation; the applier then retries it until the order is Reserved.
/// </summary>
public sealed class FulfillmentStartedEventHandler(OrderStatusEventApplier applier) : IIntegrationEventHandler<FulfillmentStartedEvent>
{
    public Task HandleAsync(FulfillmentStartedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return applier.ApplyAsync(
            integrationEvent.OrderId,
            OrderStatus.Picking,
            $"Fulfillment started in {integrationEvent.WarehouseCode}",
            FulfillmentStartedEvent.EventType,
            cancellationToken);
    }
}
