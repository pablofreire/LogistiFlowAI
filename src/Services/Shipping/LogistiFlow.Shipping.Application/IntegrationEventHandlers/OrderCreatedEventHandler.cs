using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Interfaces;

namespace LogistiFlow.Shipping.Application.IntegrationEventHandlers;

public sealed class OrderCreatedEventHandler(IRecordOrderDestinationUseCase useCase) : IIntegrationEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var result = await useCase.ExecuteAsync(
            new RecordOrderDestinationRequest(
                integrationEvent.OrderId,
                integrationEvent.OrderNumber,
                integrationEvent.CustomerName,
                integrationEvent.DestinationCity,
                integrationEvent.DestinationCountry,
                integrationEvent.PromisedDeliveryDate),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Recording the destination of {integrationEvent.OrderNumber} failed: {result.Error.Code}");
        }
    }
}
