using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.IntegrationEventHandlers;

public sealed class FulfillmentShippedEventHandler(OrderStatusEventApplier applier) : IIntegrationEventHandler<FulfillmentShippedEvent>
{
    public Task HandleAsync(FulfillmentShippedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return applier.ApplyAsync(
            integrationEvent.OrderId,
            OrderStatus.Shipped,
            $"Shipped from {integrationEvent.WarehouseCode}",
            FulfillmentShippedEvent.EventType,
            cancellationToken);
    }
}
