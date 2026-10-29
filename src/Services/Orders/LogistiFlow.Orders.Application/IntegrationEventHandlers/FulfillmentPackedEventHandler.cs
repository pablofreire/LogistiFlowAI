using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.IntegrationEventHandlers;

public sealed class FulfillmentPackedEventHandler(OrderStatusEventApplier applier) : IIntegrationEventHandler<FulfillmentPackedEvent>
{
    public Task HandleAsync(FulfillmentPackedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return applier.ApplyAsync(
            integrationEvent.OrderId,
            OrderStatus.Packed,
            $"Packed in {integrationEvent.WarehouseCode}",
            FulfillmentPackedEvent.EventType,
            cancellationToken);
    }
}
