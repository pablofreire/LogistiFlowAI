namespace LogistiFlow.Orders.Infrastructure.Persistence.Rows;

internal sealed class OrderLineRow
{
    public int LineNumber { get; init; }

    public string Sku { get; init; } = string.Empty;

    public int Quantity { get; init; }
}
