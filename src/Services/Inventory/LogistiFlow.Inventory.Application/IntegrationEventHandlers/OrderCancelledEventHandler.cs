using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Interfaces;

namespace LogistiFlow.Inventory.Application.IntegrationEventHandlers;

public sealed class OrderCancelledEventHandler(IReleaseInventoryUseCase useCase) : IIntegrationEventHandler<OrderCancelledEvent>
{
    public async Task HandleAsync(OrderCancelledEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var result = await useCase.ExecuteAsync(new ReleaseInventoryRequest(integrationEvent.OrderId, integrationEvent.OrderNumber), cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Release for {integrationEvent.OrderNumber} failed unexpectedly: {result.Error.Code}");
        }
    }
}
