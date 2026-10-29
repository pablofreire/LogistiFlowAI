namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class TotalsRow
{
    public int Products { get; init; }

    public int Warehouses { get; init; }

    public long TotalOnHand { get; init; }

    public long TotalReserved { get; init; }

    public long TotalAvailable { get; init; }

    public int LowStockPositions { get; init; }
}
