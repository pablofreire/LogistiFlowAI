using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Orders.Application.Mappings;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Interfaces;
using LogistiFlow.Orders.Domain.Entities;
using LogistiFlow.Orders.Domain.Errors;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Create.Implementations;

/// <summary>
/// Accepts an order exactly once per idempotency key. The order, its history and the <c>OrderCreated</c> event
/// are written in one transaction, so the event is published if and only if the order exists.
/// </summary>
public sealed class CreateOrderUseCase(
    IValidator<CreateOrderRequest> validator,
    IOrderRepository repository,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICreateOrderUseCase
{
    public async Task<Result<CreateOrderResponse>> ExecuteAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        var requestHash = RequestHasher.Hash(request);
        var existing = await repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Replay(existing, requestHash);
        }

        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                var order = OrderEntity.Create(
                    request.CustomerName.Trim(),
                    request.DestinationCity.Trim(),
                    request.DestinationCountry.Trim(),
                    request.PromisedDeliveryDate,
                    request.Lines.Select(line => (line.Sku.Trim().ToUpperInvariant(), line.Quantity)),
                    request.IdempotencyKey,
                    requestHash,
                    timeProvider.GetUtcNow());

                if (!await repository.TryAddAsync(order, token))
                {
                    // A concurrent request with the same key committed first; answer with its order.
                    var winner = await repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, token)
                        ?? throw new InvalidOperationException("Idempotency conflict reported but no order was found.");
                    return Replay(winner, requestHash);
                }

                await outbox.EnqueueAsync(
                    new OrderCreatedEvent(order.PublicId, order.OrderNumber, order.ToLineItems(), order.CreatedAt),
                    token);

                return Result.Success(new CreateOrderResponse(order.ToDetails(), Replayed: false));
            },
            cancellationToken);
    }

    private static Result<CreateOrderResponse> Replay(OrderEntity existing, string requestHash) =>
        string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal)
            ? Result.Success(new CreateOrderResponse(existing.ToDetails(), Replayed: true))
            : OrderErrors.IdempotencyKeyReused;
}
