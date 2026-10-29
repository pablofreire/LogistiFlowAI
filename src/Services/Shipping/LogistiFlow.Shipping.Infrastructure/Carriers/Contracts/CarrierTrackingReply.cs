namespace LogistiFlow.Shipping.Infrastructure.Carriers.Contracts;

internal sealed record CarrierTrackingReply(string? TrackingNumber, IReadOnlyList<CarrierTrackingEventReply>? Events);
