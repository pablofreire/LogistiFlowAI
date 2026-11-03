namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>Fulfillment job counts.</summary>
public sealed record FulfillmentMetricsView(
    long TotalJobs,
    IReadOnlyDictionary<string, long> ByStatus,
    IReadOnlyList<WarehouseJobCountView> ByWarehouse);
