using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>Fulfillment load and failures per warehouse, plus low stock.</summary>
public sealed record WarehouseMetricsData(
    IReadOnlyList<WarehouseJobCountView> Warehouses,
    string? MostFailures,
    long LowStockPositions,
    int LowStockThreshold);
