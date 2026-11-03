namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>The Fulfillment metrics the copilot uses.</summary>
public sealed record FulfillmentMetricsView(long TotalJobs, IReadOnlyDictionary<string, long> ByStatus, IReadOnlyList<WarehouseJobCountView> ByWarehouse);
