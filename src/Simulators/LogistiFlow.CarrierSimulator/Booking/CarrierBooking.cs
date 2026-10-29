using LogistiFlow.CarrierSimulator.Scenarios;

namespace LogistiFlow.CarrierSimulator.Booking;

public sealed record CarrierBooking(
    string IdempotencyKey,
    string TrackingNumber,
    string CarrierCode,
    string Reference,
    CarrierScenario Scenario,
    DateOnly EstimatedDelivery,
    DateTimeOffset BookedAt,
    IReadOnlyList<ScriptedEvent> Timeline);
