using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Shipping;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.IntegrationEventHandlers;

/// <summary>
/// A return fails the order, but only once it is Shipped: an earlier status means this event overtook
/// <c>FulfillmentShipped</c>, so it is retried until the order catches up.
/// </summary>
public sealed class ShipmentReturnedEventHandler(OrderStatusEventApplier applier) : IIntegrationEventHandler<ShipmentReturnedEvent>
{
    public Task HandleAsync(ShipmentReturnedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var reason = $"Returned to sender by {integrationEvent.CarrierCode}: {integrationEvent.Reason}";
        return applier.ApplyAsync(
            integrationEvent.OrderId,
            OrderStatus.Failed,
            reason.Length <= 500 ? reason : reason[..500],
            ShipmentReturnedEvent.EventType,
            cancellationToken,
            requiredCurrent: OrderStatus.Shipped);
    }
}
