namespace LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Contracts;

public sealed record WarehouseJobCount(string WarehouseCode, long Open, long Failed, long Total);
