namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class StockPositionRow
{
    public Guid Id { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public string WarehouseCode { get; init; } = string.Empty;

    public int OnHand { get; init; }

    public int Reserved { get; init; }

    public int Available { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
