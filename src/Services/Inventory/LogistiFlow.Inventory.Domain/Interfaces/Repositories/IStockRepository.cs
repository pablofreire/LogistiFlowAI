using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.ReadModels;

namespace LogistiFlow.Inventory.Domain.Interfaces.Repositories;

/// <summary>Stock positions. Every write is a single conditional statement, so concurrent callers cannot oversell.</summary>
public interface IStockRepository
{
    /// <summary>Warehouses that can fulfil every requested SKU and quantity, ordered by warehouse code.</summary>
    Task<IReadOnlyList<WarehouseStockCandidate>> FindCandidateWarehousesAsync(
        IReadOnlyDictionary<string, int> requestedBySku,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically adds <paramref name="quantity"/> to the reserved quantity if that much is still available.
    /// Returns <c>false</c>, changing nothing, when a concurrent reservation took the stock first.
    /// </summary>
    Task<bool> TryReserveAsync(long stockPositionId, int quantity, CancellationToken cancellationToken);

    /// <summary>Returns <paramref name="quantity"/> from reserved to available.</summary>
    Task ReleaseAsync(long stockPositionId, int quantity, CancellationToken cancellationToken);

    /// <summary>Adds received units to on-hand stock, creating the position if needed. Returns <c>null</c> for an unknown SKU or warehouse.</summary>
    Task<long?> ReceiveAsync(string warehouseCode, string sku, int quantity, CancellationToken cancellationToken);

    Task RecordMovementAsync(long stockPositionId, StockMovementType type, int quantity, string referenceType, string referenceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SkuAvailability>> GetAvailabilityAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken);
}
