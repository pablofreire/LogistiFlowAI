using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;

public sealed record RecentOrderActivity(
    Guid OrderId,
    string OrderNumber,
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    string? Reason,
    string Source,
    DateTimeOffset OccurredAt);
