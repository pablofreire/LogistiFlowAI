namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class OrderDestinationRow
{
    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string? CustomerName { get; init; }

    public string DestinationCity { get; init; } = string.Empty;

    public string DestinationCountry { get; init; } = string.Empty;

    public DateTime? PromisedDeliveryDate { get; init; }
}
