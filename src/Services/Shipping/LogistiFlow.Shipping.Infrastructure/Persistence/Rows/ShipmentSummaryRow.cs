namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class ShipmentSummaryRow
{
    public Guid Id { get; init; }

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string CarrierCode { get; init; } = string.Empty;

    public string CarrierName { get; init; } = string.Empty;

    public string? TrackingNumber { get; init; }

    public string Status { get; init; } = string.Empty;

    public string DestinationCity { get; init; } = string.Empty;

    public string DestinationCountry { get; init; } = string.Empty;

    public DateTime? PromisedDeliveryDate { get; init; }

    public DateTime? EstimatedDelivery { get; init; }

    public bool IsDelayed { get; init; }

    public int OpenExceptions { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
