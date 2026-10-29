namespace LogistiFlow.Orders.Infrastructure.Persistence.Rows;

internal sealed class InsertedOrderRow
{
    public long Id { get; init; }

    public string OrderNumber { get; init; } = string.Empty;
}
