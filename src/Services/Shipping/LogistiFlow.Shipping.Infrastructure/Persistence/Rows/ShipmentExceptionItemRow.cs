namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class ShipmentExceptionItemRow
{
    public Guid Id { get; init; }

    public Guid ShipmentId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string CarrierCode { get; init; } = string.Empty;

    public string? TrackingNumber { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Details { get; init; } = string.Empty;

    public DateTimeOffset OpenedAt { get; init; }

    public DateTimeOffset? ResolvedAt { get; init; }

    public string? Resolution { get; init; }

    public string? ResolvedBy { get; init; }
}
