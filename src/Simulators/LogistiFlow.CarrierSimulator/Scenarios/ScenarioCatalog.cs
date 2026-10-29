namespace LogistiFlow.CarrierSimulator.Scenarios;

/// <summary>
/// Deterministic behaviour, chosen from the destination city the way carrier sandboxes use reserved test addresses.
/// Every other city gets <see cref="CarrierScenario.Normal"/>.
/// </summary>
public static class ScenarioCatalog
{
    /// <summary>After a delay the package is held this many steps, so the delay stays observable for a while.</summary>
    public const int DelayHoldSteps = 20;

    private static readonly Dictionary<string, CarrierScenario> SandboxCities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sandbox Delay"] = CarrierScenario.Delay,
        ["Sandbox Unavailable"] = CarrierScenario.TemporaryFailure,
        ["Sandbox Timeout"] = CarrierScenario.Timeout,
        ["Sandbox Redelivery"] = CarrierScenario.Redelivery,
        ["Sandbox Address Not Found"] = CarrierScenario.AddressNotFound,
    };

    public static IReadOnlyDictionary<string, CarrierScenario> Cities => SandboxCities;

    public static CarrierScenario ForCity(string destinationCity) =>
        SandboxCities.TryGetValue(destinationCity.Trim(), out var scenario) ? scenario : CarrierScenario.Normal;

    public static IReadOnlyList<ScriptedEvent> Timeline(CarrierScenario scenario, string carrierName, string origin, string destination)
    {
        var hub = $"{carrierName} sort hub";
        ScriptedEvent[] start =
        [
            new(0, "LABEL_CREATED", "Shipping label created", origin),
            new(1, "PICKED_UP", "Picked up by carrier", origin),
            new(2, "IN_TRANSIT", "Departed sort facility", hub),
        ];

        ScriptedEvent[] rest = scenario switch
        {
            CarrierScenario.Delay =>
            [
                new(3, "DELAYED", "Delayed by severe weather at the hub", hub, EstimateShiftDays: 2),
                new(3 + DelayHoldSteps, "IN_TRANSIT", "Departed sort facility after weather hold", hub),
                new(4 + DelayHoldSteps, "OUT_FOR_DELIVERY", "Out for delivery", destination),
                new(5 + DelayHoldSteps, "DELIVERED", "Delivered", destination),
            ],
            CarrierScenario.Redelivery =>
            [
                new(3, "OUT_FOR_DELIVERY", "Out for delivery", destination),
                new(4, "DELIVERY_ATTEMPT_FAILED", "Recipient unavailable; a second attempt is scheduled", destination),
                new(5, "OUT_FOR_DELIVERY", "Out for delivery (second attempt)", destination),
                new(6, "DELIVERED", "Delivered", destination),
            ],
            CarrierScenario.AddressNotFound =>
            [
                new(3, "OUT_FOR_DELIVERY", "Out for delivery", destination),
                new(4, "DELIVERY_ATTEMPT_FAILED", "Address could not be located", destination),
                new(5, "OUT_FOR_DELIVERY", "Out for delivery (second attempt)", destination),
                new(6, "DELIVERY_ATTEMPT_FAILED", "Address could not be located", destination),
                new(7, "RETURNED_TO_SENDER", "Undeliverable: address not located; returning to sender", hub),
            ],
            _ =>
            [
                new(3, "OUT_FOR_DELIVERY", "Out for delivery", destination),
                new(4, "DELIVERED", "Delivered", destination),
            ],
        };

        return [.. start, .. rest];
    }
}
