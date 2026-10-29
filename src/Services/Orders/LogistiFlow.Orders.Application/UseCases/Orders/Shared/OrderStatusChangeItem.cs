using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Shared;

/// <summary>One entry of an order timeline. <c>Source</c> is <c>api</c> or the integration event type that caused it.</summary>
public sealed record OrderStatusChangeItem(
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    string? Reason,
    string Source,
    DateTimeOffset OccurredAt);
