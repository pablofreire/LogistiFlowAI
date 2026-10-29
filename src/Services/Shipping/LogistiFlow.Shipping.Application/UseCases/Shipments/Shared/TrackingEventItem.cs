namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

public sealed record TrackingEventItem(
    string Code,
    string Description,
    string Location,
    DateTimeOffset OccurredAt,
    DateOnly? EstimatedDelivery,
    DateTimeOffset ReceivedAt);
