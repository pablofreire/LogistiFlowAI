namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Rows;

internal sealed class FulfillmentJobSummaryRow
{
    public Guid Id { get; init; }

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string WarehouseCode { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? StatusReason { get; init; }

    public int TotalUnits { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
