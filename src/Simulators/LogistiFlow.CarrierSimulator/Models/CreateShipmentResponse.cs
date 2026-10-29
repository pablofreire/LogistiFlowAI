namespace LogistiFlow.CarrierSimulator.Models;

public sealed record CreateShipmentResponse(string TrackingNumber, string CarrierCode, DateOnly EstimatedDelivery, DateTimeOffset BookedAt);
