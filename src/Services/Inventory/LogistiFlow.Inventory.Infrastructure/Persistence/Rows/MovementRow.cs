namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class MovementRow
{
    public string Sku { get; init; } = string.Empty;

    public string WarehouseCode { get; init; } = string.Empty;

    public string MovementType { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string ReferenceType { get; init; } = string.Empty;

    public string ReferenceId { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; }
}
