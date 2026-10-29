namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Rows;

internal sealed class FulfillmentJobRow
{
    public long Id { get; init; }

    public Guid PublicId { get; init; }

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string WarehouseCode { get; init; } = string.Empty;

    public Guid ReservationId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? StatusReason { get; init; }

    public int Version { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
