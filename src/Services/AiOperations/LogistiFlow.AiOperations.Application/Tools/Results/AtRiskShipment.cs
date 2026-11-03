namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>An undelivered shipment that may miss its promised date, and why.</summary>
public sealed record AtRiskShipment(
    Guid ShipmentId,
    string OrderNumber,
    string CarrierName,
    string? TrackingNumber,
    string Status,
    DateOnly? PromisedDeliveryDate,
    DateOnly? EstimatedDelivery,
    int? DaysLateVersusPromise,
    bool IsDelayed,
    int OpenExceptions,
    string Reason);
