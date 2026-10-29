using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Interfaces;

namespace LogistiFlow.Shipping.Application.IntegrationEventHandlers;

public sealed class FulfillmentShippedEventHandler(ICreateShipmentUseCase useCase) : IIntegrationEventHandler<FulfillmentShippedEvent>
{
    public async Task HandleAsync(FulfillmentShippedEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var result = await useCase.ExecuteAsync(
            new CreateShipmentRequest(
                integrationEvent.FulfillmentJobId,
                integrationEvent.OrderId,
                integrationEvent.OrderNumber,
                integrationEvent.WarehouseCode,
                integrationEvent.ShippedAt),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Creating the shipment for {integrationEvent.OrderNumber} failed: {result.Error.Code}");
        }
    }
}
