using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Models;
using LogistiFlow.Shipping.Application.Options;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Interfaces;
using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Errors;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;
using Microsoft.Extensions.Options;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Implementations;

/// <summary>
/// Books one claimed shipment with its carrier. The HTTP call happens before, and outside, the database transaction;
/// the shipment public id goes as the carrier's idempotency key, so retrying after a timeout cannot book twice.
/// Retries are durable (next attempt time in the database) rather than in-memory HTTP retries: a long carrier
/// outage survives restarts and is visible to operators as a <see cref="ShipmentExceptionType.CarrierUnavailable"/>.
/// </summary>
public sealed class BookShipmentUseCase(
    IShipmentRepository shipments,
    ICarrierGateway carrier,
    IUnitOfWork unitOfWork,
    IOptions<ShippingWorkerOptions> options,
    TimeProvider timeProvider) : IBookShipmentUseCase
{
    public async Task<Result<BookShipmentResponse>> ExecuteAsync(BookShipmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shipment = await shipments.GetByPublicIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return ShippingErrors.ShipmentNotFound;
        }

        if (shipment.Status != ShipmentStatus.PendingBooking)
        {
            return new BookShipmentResponse(shipment.Status, Booked: false, CarrierError: null);
        }

        var result = await carrier.BookAsync(
            new CarrierBookingRequest(
                shipment.PublicId.ToString(),
                shipment.CarrierCode,
                shipment.OrderNumber,
                shipment.WarehouseCode,
                shipment.DestinationCity,
                shipment.DestinationCountry),
            cancellationToken);

        return await unitOfWork.ExecuteAsync<BookShipmentResponse>(
            async token =>
            {
                // Reload inside the transaction: an operator may have acted while the carrier call was in flight.
                var current = await shipments.GetByPublicIdAsync(request.ShipmentId, token);
                if (current is null || current.Status != ShipmentStatus.PendingBooking)
                {
                    return new BookShipmentResponse(current?.Status ?? ShipmentStatus.PendingBooking, Booked: false, CarrierError: null);
                }

                var schedule = Apply(current, result, request.Attempt);
                if (!await shipments.TryUpdateAsync(current, schedule, token))
                {
                    return ShippingErrors.ConcurrentModification;
                }

                return new BookShipmentResponse(current.Status, result.Succeeded, result.Error);
            },
            cancellationToken);
    }

    private ShipmentSchedule Apply(ShipmentEntity shipment, CarrierBookingResult result, int attempt)
    {
        var now = timeProvider.GetUtcNow();
        if (result.Succeeded && shipment.MarkBooked(result.TrackingNumber!, result.EstimatedDelivery!.Value, now).IsSuccess)
        {
            return ShipmentSchedule.PollIn(options.Value.TrackingPollInterval);
        }

        shipment.RecordBookingFailure(result.Error ?? "The carrier returned an incomplete booking.", attempt, options.Value.BookingMaxAttempts, now);
        return shipment.Status == ShipmentStatus.BookingFailed
            ? ShipmentSchedule.None
            : ShipmentSchedule.BookIn(options.Value.BookingRetryDelay(attempt));
    }
}
