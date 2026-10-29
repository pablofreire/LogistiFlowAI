using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Interfaces;
using LogistiFlow.Fulfillment.Domain.Entities;
using LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Implementations;

/// <summary>
/// Creates the fulfillment job for a reserved order and releases it for picking. One job per order: a second
/// call for the same order returns the existing job, which keeps the step idempotent beyond the inbox.
/// </summary>
public sealed class StartFulfillmentUseCase(
    IFulfillmentJobRepository jobs,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IStartFulfillmentUseCase
{
    public Task<Result<StartFulfillmentResponse>> ExecuteAsync(StartFulfillmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return unitOfWork.ExecuteAsync<StartFulfillmentResponse>(
            async token =>
            {
                var existing = await jobs.GetByOrderIdAsync(request.OrderId, token);
                if (existing is not null)
                {
                    return new StartFulfillmentResponse(existing.PublicId, existing.Status, AlreadyExisted: true);
                }

                var now = timeProvider.GetUtcNow();
                var job = FulfillmentJobEntity.CreateAndStart(
                    request.OrderId,
                    request.OrderNumber,
                    request.WarehouseCode,
                    request.ReservationId,
                    request.Lines.Select(line => new FulfillmentJobLineEntity(line.Sku, line.Quantity)),
                    request.Source,
                    now);

                if (!await jobs.TryAddAsync(job, token))
                {
                    throw new InvalidOperationException($"A fulfillment job for order {request.OrderNumber} was created concurrently.");
                }

                await outbox.EnqueueAsync(
                    new FulfillmentStartedEvent(job.PublicId, request.OrderId, request.OrderNumber, request.WarehouseCode, now),
                    token);

                return new StartFulfillmentResponse(job.PublicId, job.Status, AlreadyExisted: false);
            },
            cancellationToken);
    }
}
