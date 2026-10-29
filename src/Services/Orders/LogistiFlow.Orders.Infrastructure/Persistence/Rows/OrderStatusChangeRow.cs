namespace LogistiFlow.Orders.Infrastructure.Persistence.Rows;

internal sealed class OrderStatusChangeRow
{
    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string? FromStatus { get; init; }

    public string ToStatus { get; init; } = string.Empty;

    public string? Reason { get; init; }

    public string Source { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; }
}
