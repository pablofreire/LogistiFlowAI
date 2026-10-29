namespace LogistiFlow.CarrierSimulator.Models;

/// <param name="EventId">Stable per event; clients use it to de-duplicate repeated polls.</param>
/// <param name="Code">Carrier event code.</param>
/// <param name="Description">Human-readable text.</param>
/// <param name="Location">Where it happened.</param>
/// <param name="OccurredAt">When it happened.</param>
/// <param name="EstimatedDelivery">The delivery estimate after this event.</param>
public sealed record TrackingEventResponse(
    string EventId,
    string Code,
    string Description,
    string Location,
    DateTimeOffset OccurredAt,
    DateOnly EstimatedDelivery);
