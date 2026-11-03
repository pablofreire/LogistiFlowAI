using LogistiFlow.AiOperations.Application.Tools.Rules;
using LogistiFlow.AiOperations.UnitTests.Fixtures;

namespace LogistiFlow.AiOperations.UnitTests.Application.Tools.Rules;

public sealed class ShipmentRiskRulesTests
{
    [Theory]
    [InlineData("2026-11-06", "2026-11-08", 2)]
    [InlineData("2026-11-06", "2026-11-06", 0)]
    [InlineData("2026-11-06", "2026-11-04", -2)]
    public void DaysLate_ComparesTheEstimateWithThePromise(string promised, string estimate, int expected) =>
        ShipmentRiskRules.DaysLateVersusPromise(DateOnly.Parse(promised), DateOnly.Parse(estimate), null).Should().Be(expected);

    [Fact]
    public void DaysLate_UsesTheDeliveryOnceDelivered() =>
        ShipmentRiskRules.DaysLateVersusPromise(new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 10), new DateTimeOffset(2026, 11, 7, 9, 0, 0, TimeSpan.Zero))
            .Should().Be(1);

    [Fact]
    public void DaysLate_IsUnknownWithoutAPromise() =>
        ShipmentRiskRules.DaysLateVersusPromise(null, new DateOnly(2026, 11, 8), null).Should().BeNull();

    [Fact]
    public void Analyze_ReportsTheOpenDelay_AndHowLateTheEstimateIs()
    {
        var analysis = ShipmentRiskRules.Analyze(OperationsData.Shipment(exceptions: [OperationsData.Delay(), OperationsData.Delay("Resolved")]));

        analysis.Assessment.Should().Be("Delayed: the carrier reported a delay and the estimate is 2 day(s) after the promised date.");
        analysis.DaysLateVersusPromise.Should().Be(2);
        analysis.OpenExceptions.Should().ContainSingle().Which.Details.Should().Contain("severe weather");
        analysis.LatestEvent!.Code.Should().Be("DELAYED");
    }

    [Theory]
    [InlineData("InTransit", false, "2026-11-06", "On schedule.")]
    [InlineData("InTransit", false, "2026-11-09", "At risk: the current estimate is 3 day(s) after the promised date.")]
    [InlineData("ReturnedToSender", false, "2026-11-06", "Returned to sender; the order failed.")]
    public void Analyze_AssessesEveryState(string status, bool isDelayed, string estimate, string expected) =>
        ShipmentRiskRules.Analyze(OperationsData.Shipment(status, isDelayed, estimate: DateOnly.Parse(estimate))).Assessment.Should().Be(expected);

    [Fact]
    public void AtRiskReason_SkipsFinalAndOnTimeShipments()
    {
        ShipmentRiskRules.AtRiskReason(OperationsData.ListItem("ORD-1", "Delivered", true, new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 9))).Should().BeNull();
        ShipmentRiskRules.AtRiskReason(OperationsData.ListItem("ORD-2", "InTransit", false, new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 6))).Should().BeNull();
        ShipmentRiskRules.AtRiskReason(OperationsData.ListItem("ORD-3", "InTransit", false, new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 7)))
            .Should().Be("Estimate 1 day(s) after the promised date");
        ShipmentRiskRules.AtRiskReason(OperationsData.ListItem("ORD-4", "InTransit", true, null, null)).Should().Be("Open carrier delay");
    }
}
