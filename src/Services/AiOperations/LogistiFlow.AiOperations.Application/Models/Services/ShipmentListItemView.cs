namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>A shipment row of the Shipping list.</summary>
public sealed record ShipmentListItemView(
    Guid Id,
    string OrderNumber,
    string CarrierCode,
    string CarrierName,
    string? TrackingNumber,
    string Status,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    DateOnly? EstimatedDelivery,
    bool IsDelayed,
    int OpenExceptions);
