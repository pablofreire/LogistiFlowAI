using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.IntegrationEventHandlers;

public sealed class InventoryReservedEventHandler(OrderStatusEventApplier applier) : IIntegrationEventHandler<InventoryReservedEvent>
{
    public Task HandleAsync(InventoryReservedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return applier.ApplyAsync(
            integrationEvent.OrderId,
            OrderStatus.Reserved,
            $"Inventory reserved in {integrationEvent.WarehouseCode}",
            InventoryReservedEvent.EventType,
            cancellationToken);
    }
}
