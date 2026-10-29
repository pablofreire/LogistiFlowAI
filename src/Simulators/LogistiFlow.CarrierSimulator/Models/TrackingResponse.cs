namespace LogistiFlow.CarrierSimulator.Models;

public sealed record TrackingResponse(
    string TrackingNumber,
    string CarrierCode,
    string Status,
    DateOnly EstimatedDelivery,
    IReadOnlyList<TrackingEventResponse> Events);
