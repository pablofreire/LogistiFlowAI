namespace LogistiFlow.Shipping.Domain.Services;

/// <summary>
/// Assigns a carrier: international shipments go with GlobalPost; domestic ones go express only when the promised
/// date is close, because express costs more; everything else goes ground.
/// </summary>
public static class CarrierSelectionPolicy
{
    public const string GlobalPost = "GLBL";
    public const string SimExpress = "SIMX";
    public const string RoadrunnerGround = "RDRN";

    /// <summary>Promised dates within this many days of shipping need the express carrier.</summary>
    public const int ExpressWindowDays = 2;

    public static string Select(string destinationCountry, DateOnly? promisedDeliveryDate, DateOnly shipDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationCountry);

        if (!string.Equals(destinationCountry.Trim(), "US", StringComparison.OrdinalIgnoreCase))
        {
            return GlobalPost;
        }

        return promisedDeliveryDate is { } promised && promised.DayNumber - shipDate.DayNumber <= ExpressWindowDays
            ? SimExpress
            : RoadrunnerGround;
    }
}
