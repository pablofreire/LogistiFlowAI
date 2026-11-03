using LogistiFlow.McpServer.Contracts.Views;

namespace LogistiFlow.McpServer.Tools.Results;

/// <summary>Fulfillment jobs per warehouse and status, with the inventory totals and low-stock count.</summary>
public sealed record WarehouseMetricsResult(
    long TotalJobs,
    IReadOnlyDictionary<string, long> JobsByStatus,
    IReadOnlyList<WarehouseJobCountView> Warehouses,
    InventoryMetricsView Inventory);
