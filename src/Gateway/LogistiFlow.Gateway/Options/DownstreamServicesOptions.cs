namespace LogistiFlow.Gateway.Options;

/// <summary>Base addresses of the services the dashboard aggregates. The proxy routes use the same values.</summary>
public sealed class DownstreamServicesOptions
{
    public const string SectionName = "Services";

    public Uri Orders { get; set; } = new("http://localhost:5101");

    public Uri Inventory { get; set; } = new("http://localhost:5102");

    public Uri Fulfillment { get; set; } = new("http://localhost:5103");

    public Uri Shipping { get; set; } = new("http://localhost:5104");
}
