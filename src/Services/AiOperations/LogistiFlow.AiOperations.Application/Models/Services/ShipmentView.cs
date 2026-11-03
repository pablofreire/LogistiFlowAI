namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>A shipment with its tracking and exceptions, from the Shipping service.</summary>
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
