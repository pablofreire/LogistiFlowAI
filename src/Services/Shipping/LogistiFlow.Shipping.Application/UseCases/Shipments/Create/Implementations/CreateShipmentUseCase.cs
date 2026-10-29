using LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Interfaces;
using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Implementations;

/// <summary>
/// Opens a shipment for a shipped order and assigns its carrier. No carrier call happens here: this runs inside the
/// message transaction, so the booking is left to the booking worker (due immediately).
/// </summary>
public sealed class CreateShipmentUseCase(
    IShipmentRepository shipments,
    IOrderDestinationRepository destinations,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICreateShipmentUseCase
{
    public Task<Result<CreateShipmentResponse>> ExecuteAsync(CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return unitOfWork.ExecuteAsync<CreateShipmentResponse>(
            async token =>
            {
                var existing = await shipments.GetByOrderIdAsync(request.OrderId, token);
                if (existing is not null)
                {
                    return new CreateShipmentResponse(existing.PublicId, existing.CarrierCode, AlreadyExisted: true);
                }

                // The destination comes from OrderCreated, a different stream; this event can overtake it.
                var destination = await destinations.GetAsync(request.OrderId, token)
                    ?? throw new TransientIntegrationEventException($"The destination of order {request.OrderNumber} is not known yet.");

                var shipment = ShipmentEntity.CreatePendingBooking(
                    destination,
                    request.FulfillmentJobId,
                    request.WarehouseCode,
                    request.ShippedAt,
                    timeProvider.GetUtcNow());

                if (!await shipments.TryAddAsync(shipment, ShipmentSchedule.BookIn(TimeSpan.Zero), token))
                {
                    throw new TransientIntegrationEventException($"A shipment for {request.OrderNumber} was created concurrently.");
                }

                return new CreateShipmentResponse(shipment.PublicId, shipment.CarrierCode, AlreadyExisted: false);
            },
            cancellationToken);
    }
}
