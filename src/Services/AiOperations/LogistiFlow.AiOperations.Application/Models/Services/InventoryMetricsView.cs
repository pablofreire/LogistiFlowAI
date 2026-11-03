namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>The Inventory metrics the copilot uses.</summary>
public sealed record InventoryMetricsView(long TotalAvailable, long LowStockPositions, int LowStockThreshold);
