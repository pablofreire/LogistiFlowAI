using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;
using LogistiFlow.Shipping.Domain.Errors;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Implementations;

/// <summary>Reprocessing after the booking gave up: back to PendingBooking, due now, with a fresh attempt budget.</summary>
public sealed class RetryShipmentBookingUseCase(
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRetryShipmentBookingUseCase
{
    public Task<Result<ShipmentActionOutcome>> ExecuteAsync(RetryShipmentBookingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return unitOfWork.ExecuteAsync<ShipmentActionOutcome>(
            async token =>
            {
                var shipment = await shipments.GetByPublicIdAsync(request.ShipmentId, token);
                if (shipment is null)
                {
                    return ShippingErrors.ShipmentNotFound;
                }

                var retried = shipment.RetryBooking(timeProvider.GetUtcNow());
                if (retried.IsFailure)
                {
                    return retried.Error;
                }

                if (!await shipments.TryUpdateAsync(shipment, ShipmentSchedule.BookIn(TimeSpan.Zero), token))
                {
                    return ShippingErrors.ConcurrentModification;
                }

                return new ShipmentActionOutcome(shipment.PublicId, shipment.OrderNumber, shipment.Status, shipment.StatusReason, shipment.Version);
            },
            cancellationToken);
    }
}
