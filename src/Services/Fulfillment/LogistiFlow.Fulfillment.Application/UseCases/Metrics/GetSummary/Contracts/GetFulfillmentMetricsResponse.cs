namespace LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Contracts;

/// <summary>Fulfillment KPIs: job counts per status, overall and per warehouse.</summary>
public sealed record GetFulfillmentMetricsResponse(
    long TotalJobs,
    IReadOnlyDictionary<string, long> ByStatus,
    IReadOnlyList<WarehouseJobCount> ByWarehouse);
