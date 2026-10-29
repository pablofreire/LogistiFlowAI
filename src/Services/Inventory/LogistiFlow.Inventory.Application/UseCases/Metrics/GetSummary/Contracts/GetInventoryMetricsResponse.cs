namespace LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;

/// <summary>Inventory KPIs. <c>LowStockPositions</c> counts positions whose available quantity is at or below the threshold.</summary>
public sealed record GetInventoryMetricsResponse(
    int Products,
    int Warehouses,
    long TotalOnHand,
    long TotalReserved,
    long TotalAvailable,
    int LowStockPositions,
    int LowStockThreshold,
    IReadOnlyDictionary<string, long> ReservationsByStatus,
    IReadOnlyList<RecentStockMovement> RecentMovements);
