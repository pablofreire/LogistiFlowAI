namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>Inventory totals and the low-stock count.</summary>
public sealed record InventoryMetricsView(
    long TotalAvailable,
    long LowStockPositions,
    int LowStockThreshold);
