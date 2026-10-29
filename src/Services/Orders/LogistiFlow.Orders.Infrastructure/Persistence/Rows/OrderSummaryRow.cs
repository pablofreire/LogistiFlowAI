namespace LogistiFlow.Orders.Infrastructure.Persistence.Rows;

internal sealed class OrderSummaryRow
{
    public Guid Id { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string DestinationCity { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? StatusReason { get; init; }

    public int LineCount { get; init; }

    public int TotalUnits { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
