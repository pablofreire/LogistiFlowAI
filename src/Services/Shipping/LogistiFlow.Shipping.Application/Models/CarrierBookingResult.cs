namespace LogistiFlow.Shipping.Application.Models;

/// <summary>Outcome of one booking call. Failures are values, not exceptions: they are expected and retried.</summary>
public sealed record CarrierBookingResult(string? TrackingNumber, DateOnly? EstimatedDelivery, string? Error)
{
    public bool Succeeded => Error is null && TrackingNumber is not null && EstimatedDelivery is not null;

    public static CarrierBookingResult Booked(string trackingNumber, DateOnly estimatedDelivery) => new(trackingNumber, estimatedDelivery, null);

    public static CarrierBookingResult Failed(string error) => new(null, null, error);
}
