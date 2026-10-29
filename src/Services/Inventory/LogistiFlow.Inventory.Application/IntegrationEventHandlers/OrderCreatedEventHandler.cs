using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Interfaces;

namespace LogistiFlow.Inventory.Application.IntegrationEventHandlers;

public sealed class OrderCreatedEventHandler(IReserveInventoryUseCase useCase) : IIntegrationEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var result = await useCase.ExecuteAsync(
            new ReserveInventoryRequest(integrationEvent.OrderId, integrationEvent.OrderNumber, integrationEvent.Lines),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Reservation for {integrationEvent.OrderNumber} failed unexpectedly: {result.Error.Code}");
        }
    }
}
