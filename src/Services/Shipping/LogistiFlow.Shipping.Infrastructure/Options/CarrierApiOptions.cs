namespace LogistiFlow.Shipping.Infrastructure.Options;

public sealed class CarrierApiOptions
{
    public const string SectionName = "CarrierApi";

    /// <summary>Base address of the carrier API (the simulator in every environment of this project).</summary>
    public Uri BaseAddress { get; set; } = new("http://localhost:5190");

    /// <summary>Per-call timeout. A slower answer counts as a failed attempt and is retried later.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
