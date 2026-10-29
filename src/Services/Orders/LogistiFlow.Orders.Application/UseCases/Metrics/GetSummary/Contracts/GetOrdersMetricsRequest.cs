namespace LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;

public sealed record GetOrdersMetricsRequest(int RecentActivityLimit = 10);
