namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class TrackingEventRow
{
    public string Code { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; }

    public DateTime? EstimatedDelivery { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }
}
