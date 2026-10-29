using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.Application.Models;

/// <summary>The carrier's full event history for a shipment so far, or why it could not be read.</summary>
public sealed record CarrierTrackingResult(IReadOnlyList<CarrierTrackingEvent> Events, string? Error)
{
    public bool Succeeded => Error is null;

    public static CarrierTrackingResult Read(IReadOnlyList<CarrierTrackingEvent> events) => new(events, null);

    public static CarrierTrackingResult Failed(string error) => new([], error);
}
