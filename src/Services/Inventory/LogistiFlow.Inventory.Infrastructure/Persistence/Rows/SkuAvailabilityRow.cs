namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class SkuAvailabilityRow
{
    public string Sku { get; init; } = string.Empty;

    public int BestWarehouseAvailable { get; init; }

    public int NetworkAvailable { get; init; }
}
