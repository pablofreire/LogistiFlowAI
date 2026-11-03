using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Implementations;
using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Implementations;

/// <summary>
/// Compensation for a cancelled order. Releases held stock; when the cancellation overtakes the reservation, it
/// writes a tombstone so the late <c>OrderCreated</c> reserves nothing.
/// </summary>
public sealed class ReleaseInventoryUseCase(
    IStockRepository stock,
    IReservationRepository reservations,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IReleaseInventoryUseCase
{
    public Task<Result<ReleaseInventoryResponse>> ExecuteAsync(ReleaseInventoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return unitOfWork.ExecuteAsync(token => ReleaseAsync(request, token), cancellationToken);
    }

    private async Task<Result<ReleaseInventoryResponse>> ReleaseAsync(ReleaseInventoryRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var reservation = await reservations.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (reservation is null)
        {
            await reservations.TryAddAsync(ReservationEntity.CancelledBeforeReservation(request.OrderId, request.OrderNumber, now), cancellationToken);
            return new ReleaseInventoryResponse(ReservationStatus.Cancelled, 0);
        }

        if (!reservation.Release(now))
        {
            return new ReleaseInventoryResponse(reservation.Status, 0);
        }

        foreach (var line in reservation.Lines.OrderBy(line => line.StockPositionId))
        {
            await stock.ReleaseAsync(line.StockPositionId, line.Quantity, cancellationToken);
            await stock.RecordMovementAsync(line.StockPositionId, StockMovementType.Release, line.Quantity, ReserveInventoryUseCase.MovementReferenceType, request.OrderNumber, cancellationToken);
        }

        await reservations.UpdateStatusAsync(reservation, cancellationToken);
        await outbox.EnqueueAsync(new InventoryReleasedEvent(reservation.PublicId, request.OrderId, request.OrderNumber, now), cancellationToken);
        return new ReleaseInventoryResponse(ReservationStatus.Released, reservation.Lines.Sum(line => line.Quantity));
    }
}
