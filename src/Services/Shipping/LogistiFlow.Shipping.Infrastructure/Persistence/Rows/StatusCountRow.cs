namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class StatusCountRow
{
    public string Status { get; init; } = string.Empty;

    public long Count { get; init; }
}
