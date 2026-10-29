namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Rows;

internal sealed class WarehouseJobCountRow
{
    public string WarehouseCode { get; init; } = string.Empty;

    public long Open { get; init; }

    public long Failed { get; init; }

    public long Total { get; init; }
}
