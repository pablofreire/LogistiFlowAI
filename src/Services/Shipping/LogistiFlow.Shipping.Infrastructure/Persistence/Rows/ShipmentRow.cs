namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class ShipmentRow
{
    public long Id { get; init; }

    public Guid PublicId { get; init; }

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public Guid FulfillmentJobId { get; init; }

    public string WarehouseCode { get; init; } = string.Empty;

    public string DestinationCity { get; init; } = string.Empty;

    public string DestinationCountry { get; init; } = string.Empty;

    public DateTime? PromisedDeliveryDate { get; init; }

    public string CarrierCode { get; init; } = string.Empty;

    public string? TrackingNumber { get; init; }

    public DateTime? OriginalEstimatedDelivery { get; init; }

    public DateTime? EstimatedDelivery { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? StatusReason { get; init; }

    public DateTimeOffset? DeliveredAt { get; init; }

    public int BookingAttempts { get; init; }

    public int Version { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
