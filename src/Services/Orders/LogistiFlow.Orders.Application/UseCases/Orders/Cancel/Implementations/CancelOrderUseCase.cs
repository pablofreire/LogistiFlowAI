using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Orders.Application.Mappings;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;
using LogistiFlow.Orders.Domain.Errors;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Implementations;

/// <summary>
/// Cancels an order that has not started picking. <c>OrderCancelled</c> is the compensation trigger: Inventory
/// releases the reservation and Fulfillment cancels any job created in the meantime.
/// </summary>
public sealed class CancelOrderUseCase(
    IValidator<CancelOrderRequest> validator,
    IOrderRepository repository,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICancelOrderUseCase
{
    public async Task<Result<OrderDetails>> ExecuteAsync(CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                var order = await repository.GetByPublicIdAsync(request.OrderId, token);
                if (order is null)
                {
                    return OrderErrors.NotFound;
                }

                var now = timeProvider.GetUtcNow();
                var cancelled = order.Cancel(request.Reason.Trim(), now);
                if (cancelled.IsFailure)
                {
                    return cancelled.Error;
                }

                if (!await repository.TryUpdateStatusAsync(order, token))
                {
                    return OrderErrors.ConcurrentModification;
                }

                await outbox.EnqueueAsync(new OrderCancelledEvent(order.PublicId, order.OrderNumber, request.Reason.Trim(), now), token);
                return Result.Success(order.ToDetails());
            },
            cancellationToken);
    }
}
