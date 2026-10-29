namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class WarehouseAvailabilityRow
{
    public string WarehouseCode { get; init; } = string.Empty;

    public int OnHand { get; init; }

    public int Reserved { get; init; }

    public int Available { get; init; }
}
