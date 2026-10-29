namespace LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;

public sealed record GetInventoryMetricsRequest(int LowStockThreshold = 10, int RecentMovementLimit = 10);
