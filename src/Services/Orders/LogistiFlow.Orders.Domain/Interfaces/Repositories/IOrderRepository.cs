using LogistiFlow.Orders.Domain.Entities;

namespace LogistiFlow.Orders.Domain.Interfaces.Repositories;

/// <summary>Write-side persistence of the order aggregate. Reads for screens go through the application queries.</summary>
public interface IOrderRepository
{
    Task<OrderEntity?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);

    Task<OrderEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the order, its lines and its history. Returns <c>false</c> without throwing when another request
    /// already stored an order with the same idempotency key (a concurrent duplicate).
    /// </summary>
    Task<bool> TryAddAsync(OrderEntity order, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the new status and appends the pending history, guarded by the order version. Returns <c>false</c>
    /// when the order was changed concurrently.
    /// </summary>
    Task<bool> TryUpdateStatusAsync(OrderEntity order, CancellationToken cancellationToken);
}
