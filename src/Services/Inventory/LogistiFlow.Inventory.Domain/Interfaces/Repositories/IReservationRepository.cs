using LogistiFlow.Inventory.Domain.Entities;

namespace LogistiFlow.Inventory.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
    Task<ReservationEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Returns <c>false</c> when a reservation already exists for the order.</summary>
    Task<bool> TryAddAsync(ReservationEntity reservation, CancellationToken cancellationToken);

    Task UpdateStatusAsync(ReservationEntity reservation, CancellationToken cancellationToken);
}
