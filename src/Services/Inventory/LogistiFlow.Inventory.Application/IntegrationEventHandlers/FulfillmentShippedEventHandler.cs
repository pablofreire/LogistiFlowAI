using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Interfaces;

namespace LogistiFlow.Inventory.Application.IntegrationEventHandlers;

public sealed class FulfillmentShippedEventHandler(IConsumeReservationUseCase useCase) : IIntegrationEventHandler<FulfillmentShippedEvent>
{
    public async Task HandleAsync(FulfillmentShippedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var result = await useCase.ExecuteAsync(new ConsumeReservationRequest(integrationEvent.OrderId, integrationEvent.OrderNumber), cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Consuming stock for {integrationEvent.OrderNumber} failed: {result.Error.Code}");
        }
    }
}
