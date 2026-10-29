using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Shipping;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.IntegrationEventHandlers;

public sealed class ShipmentDeliveredEventHandler(OrderStatusEventApplier applier) : IIntegrationEventHandler<ShipmentDeliveredEvent>
{
    public Task HandleAsync(ShipmentDeliveredEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return applier.ApplyAsync(
            integrationEvent.OrderId,
            OrderStatus.Delivered,
            $"Delivered by {integrationEvent.CarrierCode} ({integrationEvent.TrackingNumber})",
            ShipmentDeliveredEvent.EventType,
            cancellationToken);
    }
}
