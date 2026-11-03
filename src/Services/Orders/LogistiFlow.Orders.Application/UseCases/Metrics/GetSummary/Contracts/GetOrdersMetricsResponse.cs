namespace LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;

/// <summary>
/// Order KPIs. <c>ByStatus</c> holds every status, zero included. Each <c>AverageSecondsTo*</c> is the mean time
/// from order creation to the first time the order reached that status (picking started, shipped, delivered), over
/// the orders that reached it; null until one has. <c>AverageSecondsToDelivered</c> is the end-to-end processing time.
/// </summary>
public sealed record GetOrdersMetricsResponse(
    long TotalOrders,
    IReadOnlyDictionary<string, long> ByStatus,
    long CreatedLast24Hours,
    double? AverageSecondsToPicking,
    double? AverageSecondsToShipped,
    double? AverageSecondsToDelivered,
    IReadOnlyList<RecentOrderActivity> RecentActivity);
