using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Domain.Entities;

/// <summary>One entry of the order's status history. Append-only.</summary>
public sealed record OrderStatusChangeEntity(
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    string? Reason,
    string Source,
    DateTimeOffset OccurredAt);
