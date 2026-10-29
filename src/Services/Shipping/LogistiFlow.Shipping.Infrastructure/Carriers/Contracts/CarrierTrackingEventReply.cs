namespace LogistiFlow.Shipping.Infrastructure.Carriers.Contracts;

internal sealed record CarrierTrackingEventReply(
    string? EventId,
    string? Code,
    string? Description,
    string? Location,
    DateTimeOffset OccurredAt,
    DateOnly? EstimatedDelivery);
