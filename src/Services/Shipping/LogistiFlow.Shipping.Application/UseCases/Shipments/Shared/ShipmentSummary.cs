using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

public sealed record ShipmentSummary(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string CarrierCode,
    string CarrierName,
    string? TrackingNumber,
    ShipmentStatus Status,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    DateOnly? EstimatedDelivery,
    bool IsDelayed,
    int OpenExceptions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
