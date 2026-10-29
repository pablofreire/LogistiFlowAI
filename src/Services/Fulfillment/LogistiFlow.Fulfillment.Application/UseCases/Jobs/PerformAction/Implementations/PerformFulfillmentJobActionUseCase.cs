using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Interfaces;
using LogistiFlow.Fulfillment.Domain.Entities;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Domain.Errors;
using LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Implementations;

/// <summary>
/// Warehouse floor actions. The status change, its history row and the integration event (packed or shipped) are
/// written in one transaction; the version check turns two operators acting on the same job into a 409.
/// </summary>
public sealed class PerformFulfillmentJobActionUseCase(
    IValidator<PerformFulfillmentJobActionRequest> validator,
    IFulfillmentJobRepository jobs,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IPerformFulfillmentJobActionUseCase
{
    public async Task<Result<PerformFulfillmentJobActionResponse>> ExecuteAsync(
        PerformFulfillmentJobActionRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        return await unitOfWork.ExecuteAsync<PerformFulfillmentJobActionResponse>(
            async token =>
            {
                var job = await jobs.GetByPublicIdAsync(request.JobId, token);
                if (job is null)
                {
                    return FulfillmentErrors.JobNotFound;
                }

                var now = timeProvider.GetUtcNow();
                var performed = job.Perform(request.Action, request.Reason, $"operator:{request.PerformedBy}", now);
                if (performed.IsFailure)
                {
                    return performed.Error;
                }

                if (!await jobs.TryUpdateStatusAsync(job, token))
                {
                    return FulfillmentErrors.ConcurrentModification;
                }

                await PublishAsync(job, now, token);
                return new PerformFulfillmentJobActionResponse(job.PublicId, job.OrderId, job.OrderNumber, job.Status, job.StatusReason, job.Version);
            },
            cancellationToken);
    }

    /// <summary>Only packing and shipping concern other services; issues and reprocessing stay inside the warehouse.</summary>
    private Task PublishAsync(FulfillmentJobEntity job, DateTimeOffset now, CancellationToken cancellationToken) => job.Status switch
    {
        FulfillmentJobStatus.Packed => outbox.EnqueueAsync(
            new FulfillmentPackedEvent(job.PublicId, job.OrderId, job.OrderNumber, job.WarehouseCode, now),
            cancellationToken),
        FulfillmentJobStatus.Shipped => outbox.EnqueueAsync(
            new FulfillmentShippedEvent(
                job.PublicId,
                job.OrderId,
                job.OrderNumber,
                job.WarehouseCode,
                job.ReservationId,
                job.Lines.Select(line => new OrderLineItem(line.Sku, line.Quantity)).ToList(),
                now),
            cancellationToken),
        _ => Task.CompletedTask,
    };
}
