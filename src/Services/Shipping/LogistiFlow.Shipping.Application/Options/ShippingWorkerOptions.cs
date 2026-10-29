namespace LogistiFlow.Shipping.Application.Options;

public sealed class ShippingWorkerOptions
{
    public const string SectionName = "ShippingWorkers";

    /// <summary>Turns both workers off; tests drive the use cases directly when they need to.</summary>
    public bool Enabled { get; set; } = true;

    public int BatchSize { get; set; } = 20;

    /// <summary>How long a claim is held; must exceed a carrier call including its timeout.</summary>
    public TimeSpan Lease { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Sleep when no shipment is due.</summary>
    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromSeconds(1);

    public int BookingMaxAttempts { get; set; } = 6;

    /// <summary>First retry delay; doubles per attempt up to <see cref="BookingMaxDelay"/>.</summary>
    public TimeSpan BookingBaseDelay { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan BookingMaxDelay { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan TrackingPollInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Exponential backoff for booking attempt <paramref name="attempt"/> (1-based).</summary>
    public TimeSpan BookingRetryDelay(int attempt)
    {
        var factor = Math.Pow(2, Math.Clamp(attempt - 1, 0, 20));
        var delay = TimeSpan.FromTicks((long)Math.Min(BookingBaseDelay.Ticks * factor, BookingMaxDelay.Ticks));
        return delay;
    }
}
