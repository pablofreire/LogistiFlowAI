using LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Implementations;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Implementations;

/// <summary>
/// The shipped units leave the warehouse: on-hand and reserved stock drop together, so availability does not change.
/// The reservation status guards against a second consumption on top of the inbox.
/// </summary>
public sealed class ConsumeReservationUseCase(
    IStockRepository stock,
    IReservationRepository reservations,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IConsumeReservationUseCase
{
    public Task<Result<ConsumeReservationResponse>> ExecuteAsync(ConsumeReservationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return unitOfWork.ExecuteAsync(token => ConsumeAsync(request, token), cancellationToken);
    }

    private async Task<Result<ConsumeReservationResponse>> ConsumeAsync(ConsumeReservationRequest request, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByOrderIdAsync(request.OrderId, cancellationToken)
            ?? throw new TransientIntegrationEventException($"No reservation is visible yet for order {request.OrderNumber}.");

        if (!reservation.Consume(timeProvider.GetUtcNow()))
        {
            if (reservation.Status == ReservationStatus.Consumed)
            {
                return new ConsumeReservationResponse(ReservationStatus.Consumed, 0);
            }

            throw new InvalidOperationException(
                $"Order {request.OrderNumber} shipped, but its reservation is {reservation.Status}; stock needs a manual check.");
        }

        foreach (var line in reservation.Lines.OrderBy(line => line.StockPositionId))
        {
            await stock.ConsumeAsync(line.StockPositionId, line.Quantity, cancellationToken);
            await stock.RecordMovementAsync(
                line.StockPositionId,
                StockMovementType.Shipment,
                line.Quantity,
                ReserveInventoryUseCase.MovementReferenceType,
                request.OrderNumber,
                cancellationToken);
        }

        await reservations.UpdateStatusAsync(reservation, cancellationToken);
        return new ConsumeReservationResponse(ReservationStatus.Consumed, reservation.Lines.Sum(line => line.Quantity));
    }
}
