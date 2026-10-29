using LogistiFlow.Shipping.Application.UseCases.Exceptions.Shared;
using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

public sealed record ShipmentDetails(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    Guid FulfillmentJobId,
    string WarehouseCode,
    string CarrierCode,
    string CarrierName,
    string? TrackingNumber,
    ShipmentStatus Status,
    string? StatusReason,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    DateOnly? OriginalEstimatedDelivery,
    DateOnly? EstimatedDelivery,
    DateTimeOffset? DeliveredAt,
    int BookingAttempts,
    bool IsDelayed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<TrackingEventItem> TrackingEvents,
    IReadOnlyList<ShipmentExceptionItem> Exceptions);
