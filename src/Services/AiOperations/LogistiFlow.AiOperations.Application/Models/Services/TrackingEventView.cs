namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>A carrier tracking event.</summary>
public sealed record TrackingEventView(
    string Code,
    string Description,
    string Location,
    DateTimeOffset OccurredAt,
    DateOnly? EstimatedDelivery);
