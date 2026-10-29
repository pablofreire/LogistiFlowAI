namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Rows;

internal sealed class FulfillmentJobStatusChangeRow
{
    public string? FromStatus { get; init; }

    public string ToStatus { get; init; } = string.Empty;

    public string? Reason { get; init; }

    public string Source { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; }
}
