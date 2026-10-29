using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Shared;
using LogistiFlow.Inventory.Domain.Errors;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Implementations;

public sealed class GetReservationByOrderUseCase(IInventoryQueries queries) : IGetReservationByOrderUseCase
{
    public async Task<Result<ReservationDetails>> ExecuteAsync(GetReservationByOrderRequest request, CancellationToken cancellationToken)
    {
        var reservation = await queries.GetReservationByOrderAsync(request.OrderId, cancellationToken);
        return reservation is null ? InventoryErrors.ReservationNotFound : reservation;
    }
}
