namespace LogistiFlow.Orders.Domain.Entities;

/// <summary>A line of an order. Owned by <see cref="OrderEntity"/>; never addressed on its own.</summary>
public sealed class OrderLineEntity
{
    public OrderLineEntity(int lineNumber, string sku, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        LineNumber = lineNumber;
        Sku = sku;
        Quantity = quantity;
    }

    public int LineNumber { get; }

    public string Sku { get; }

    public int Quantity { get; }
}
