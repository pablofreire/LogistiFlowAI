namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class ReservationRow
{
    public long Id { get; init; }

    public Guid PublicId { get; init; }

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string? WarehouseCode { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? FailureReason { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
