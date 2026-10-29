namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class ReservationLineRow
{
    public long StockPositionId { get; init; }

    public string Sku { get; init; } = string.Empty;

    public int Quantity { get; init; }
}
