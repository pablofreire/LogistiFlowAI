using LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Interfaces;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Implementations;

/// <summary>
/// Compensation: an order cancelled after its reservation may already have a job on the floor. A job that
/// has not been packed is cancelled; a packed or shipped job is left untouched.
/// </summary>
public sealed class CancelFulfillmentForOrderUseCase(
    IFulfillmentJobRepository jobs,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICancelFulfillmentForOrderUseCase
{
    public Task<Result<CancelFulfillmentForOrderResponse>> ExecuteAsync(CancelFulfillmentForOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return unitOfWork.ExecuteAsync<CancelFulfillmentForOrderResponse>(
            async token =>
            {
                var job = await jobs.GetByOrderIdAsync(request.OrderId, token);
                if (job is null)
                {
                    return new CancelFulfillmentForOrderResponse(false, null);
                }

                if (job.TransitionTo(FulfillmentJobStatus.Cancelled, request.Reason, request.Source, timeProvider.GetUtcNow()).IsFailure)
                {
                    return new CancelFulfillmentForOrderResponse(false, job.Status);
                }

                if (!await jobs.TryUpdateStatusAsync(job, token))
                {
                    throw new TransientIntegrationEventException($"Fulfillment job for {job.OrderNumber} changed concurrently.");
                }

                return new CancelFulfillmentForOrderResponse(true, job.Status);
            },
            cancellationToken);
    }
}
