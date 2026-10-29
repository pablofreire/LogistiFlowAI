namespace LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;

/// <summary>
/// Order KPIs. <c>ByStatus</c> holds every status, zero included. <c>AverageSecondsToPicking</c> is the mean time
/// from order creation to the start of picking, over the orders that reached it.
/// </summary>
public sealed record GetOrdersMetricsResponse(
    long TotalOrders,
    IReadOnlyDictionary<string, long> ByStatus,
    long CreatedLast24Hours,
    double? AverageSecondsToPicking,
    IReadOnlyList<RecentOrderActivity> RecentActivity);
