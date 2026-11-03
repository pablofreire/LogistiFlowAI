namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>A shipment with its carrier tracking and operational exceptions.</summary>
public sealed record ShipmentView(
    Guid Id,
    string OrderNumber,
    string WarehouseCode,
    string CarrierCode,
    string CarrierName,
    string? TrackingNumber,
    string Status,
    string? StatusReason,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    DateOnly? OriginalEstimatedDelivery,
    DateOnly? EstimatedDelivery,
    DateTimeOffset? DeliveredAt,
    int BookingAttempts,
    bool IsDelayed,
    IReadOnlyList<TrackingEventView> TrackingEvents,
    IReadOnlyList<ShipmentExceptionView> Exceptions);
