using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Interfaces;

namespace LogistiFlow.Fulfillment.Application.IntegrationEventHandlers;

public sealed class InventoryReservedEventHandler(IStartFulfillmentUseCase useCase) : IIntegrationEventHandler<InventoryReservedEvent>
{
    public async Task HandleAsync(InventoryReservedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var result = await useCase.ExecuteAsync(
            new StartFulfillmentRequest(
                integrationEvent.ReservationId,
                integrationEvent.OrderId,
                integrationEvent.OrderNumber,
                integrationEvent.WarehouseCode,
                integrationEvent.Lines,
                InventoryReservedEvent.EventType),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Starting fulfillment for {integrationEvent.OrderNumber} failed: {result.Error.Code}");
        }
    }
}
