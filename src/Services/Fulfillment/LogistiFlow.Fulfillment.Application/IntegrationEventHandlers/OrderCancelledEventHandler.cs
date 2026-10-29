using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Interfaces;

namespace LogistiFlow.Fulfillment.Application.IntegrationEventHandlers;

public sealed class OrderCancelledEventHandler(ICancelFulfillmentForOrderUseCase useCase) : IIntegrationEventHandler<OrderCancelledEvent>
{
    public async Task HandleAsync(OrderCancelledEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var result = await useCase.ExecuteAsync(
            new CancelFulfillmentForOrderRequest(integrationEvent.OrderId, $"Order cancelled: {integrationEvent.Reason}", OrderCancelledEvent.EventType),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Cancelling fulfillment for {integrationEvent.OrderNumber} failed: {result.Error.Code}");
        }
    }
}
