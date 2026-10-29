namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class CandidatePositionRow
{
    public string WarehouseCode { get; init; } = string.Empty;

    public long StockPositionId { get; init; }

    public string Sku { get; init; } = string.Empty;

    public int Available { get; init; }
}
