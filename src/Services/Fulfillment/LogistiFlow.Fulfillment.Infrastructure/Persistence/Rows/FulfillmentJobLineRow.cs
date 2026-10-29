namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Rows;

internal sealed class FulfillmentJobLineRow
{
    public string Sku { get; init; } = string.Empty;

    public int Quantity { get; init; }
}
