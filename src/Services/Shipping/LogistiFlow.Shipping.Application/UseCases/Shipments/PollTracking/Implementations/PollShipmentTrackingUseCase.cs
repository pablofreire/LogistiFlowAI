using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Shipping;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Options;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Interfaces;
using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Errors;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;
using LogistiFlow.Shipping.Domain.Services;
using Microsoft.Extensions.Options;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Implementations;

/// <summary>
/// Reads the carrier's tracking history and applies the events not seen before. The carrier returns the whole
/// history every time; the unique carrier event id makes that harmless. Delivery and return are published to the
/// other services through the outbox, in the same transaction as the status change.
/// </summary>
public sealed class PollShipmentTrackingUseCase(
    IShipmentRepository shipments,
    IShipmentWorkQueue workQueue,
    ICarrierGateway carrier,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    IOptions<ShippingWorkerOptions> options,
    TimeProvider timeProvider) : IPollShipmentTrackingUseCase
{
    public async Task<Result<PollShipmentTrackingResponse>> ExecuteAsync(PollShipmentTrackingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shipment = await shipments.GetByPublicIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return ShippingErrors.ShipmentNotFound;
        }

        if (!ShipmentStateMachine.IsTracked(shipment.Status) || shipment.TrackingNumber is null)
        {
            return new PollShipmentTrackingResponse(shipment.Status, 0, null);
        }

        var tracking = await carrier.GetTrackingAsync(shipment.CarrierCode, shipment.TrackingNumber, cancellationToken);
        var pollAgain = ShipmentSchedule.PollIn(options.Value.TrackingPollInterval);
        if (!tracking.Succeeded)
        {
            await workQueue.RescheduleAsync(shipment.PublicId, pollAgain, cancellationToken);
            return new PollShipmentTrackingResponse(shipment.Status, 0, tracking.Error);
        }

        return await unitOfWork.ExecuteAsync<PollShipmentTrackingResponse>(
            async token =>
            {
                var current = await shipments.GetByPublicIdAsync(request.ShipmentId, token);
                if (current is null)
                {
                    return ShippingErrors.ShipmentNotFound;
                }

                var before = current.Status;
                var newEvents = 0;
                foreach (var trackingEvent in tracking.Events.OrderBy(e => e.OccurredAt).ThenBy(e => e.CarrierEventId, StringComparer.Ordinal))
                {
                    if (await shipments.TryAddTrackingEventAsync(current.Id, trackingEvent, token))
                    {
                        current.ApplyTrackingEvent(trackingEvent, timeProvider.GetUtcNow());
                        newEvents++;
                    }
                }

                var schedule = ShipmentStateMachine.IsTracked(current.Status) ? pollAgain : ShipmentSchedule.None;
                if (newEvents == 0)
                {
                    await workQueue.RescheduleAsync(current.PublicId, schedule, token);
                    return new PollShipmentTrackingResponse(current.Status, 0, null);
                }

                if (!await shipments.TryUpdateAsync(current, schedule, token))
                {
                    return ShippingErrors.ConcurrentModification;
                }

                if (before != current.Status)
                {
                    await PublishOutcomeAsync(current, token);
                }

                return new PollShipmentTrackingResponse(current.Status, newEvents, null);
            },
            cancellationToken);
    }

    private Task PublishOutcomeAsync(ShipmentEntity shipment, CancellationToken cancellationToken) => shipment.Status switch
    {
        ShipmentStatus.Delivered => outbox.EnqueueAsync(
            new ShipmentDeliveredEvent(
                shipment.PublicId,
                shipment.OrderId,
                shipment.OrderNumber,
                shipment.CarrierCode,
                shipment.TrackingNumber!,
                shipment.DeliveredAt ?? timeProvider.GetUtcNow()),
            cancellationToken),
        ShipmentStatus.ReturnedToSender => outbox.EnqueueAsync(
            new ShipmentReturnedEvent(
                shipment.PublicId,
                shipment.OrderId,
                shipment.OrderNumber,
                shipment.CarrierCode,
                shipment.TrackingNumber!,
                shipment.StatusReason ?? "Returned to sender",
                timeProvider.GetUtcNow()),
            cancellationToken),
        _ => Task.CompletedTask,
    };
}
