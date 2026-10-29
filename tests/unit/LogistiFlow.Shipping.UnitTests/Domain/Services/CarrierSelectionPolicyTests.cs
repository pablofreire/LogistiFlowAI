using LogistiFlow.Shipping.Domain.Services;

namespace LogistiFlow.Shipping.UnitTests.Domain.Services;

public sealed class CarrierSelectionPolicyTests
{
    private static readonly DateOnly ShipDate = new(2026, 10, 29);

    [Theory]
    [InlineData("CA", null, "GLBL")]
    [InlineData("br", "2026-10-30", "GLBL")]
    [InlineData("US", null, "RDRN")]
    [InlineData("US", "2026-11-10", "RDRN")]
    [InlineData("US", "2026-11-01", "RDRN")]
    [InlineData("US", "2026-10-31", "SIMX")]
    [InlineData(" us ", "2026-10-29", "SIMX")]
    public void Select_FollowsTheDocumentedRules(string country, string? promised, string expected)
    {
        var promisedDate = promised is null ? (DateOnly?)null : DateOnly.Parse(promised, System.Globalization.CultureInfo.InvariantCulture);

        CarrierSelectionPolicy.Select(country, promisedDate, ShipDate).Should().Be(expected);
    }
}
