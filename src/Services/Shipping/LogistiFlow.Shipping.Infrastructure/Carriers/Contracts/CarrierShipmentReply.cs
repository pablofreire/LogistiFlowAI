namespace LogistiFlow.Shipping.Infrastructure.Carriers.Contracts;

internal sealed record CarrierShipmentReply(string? TrackingNumber, string? CarrierCode, DateOnly? EstimatedDelivery);
