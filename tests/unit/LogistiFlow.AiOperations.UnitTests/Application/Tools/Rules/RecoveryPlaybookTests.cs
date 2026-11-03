using LogistiFlow.AiOperations.Application.Tools.Rules;
using LogistiFlow.AiOperations.UnitTests.Fixtures;

namespace LogistiFlow.AiOperations.UnitTests.Application.Tools.Rules;

public sealed class RecoveryPlaybookTests
{
    [Fact]
    public void OpenDelay_SuggestsTellingTheCustomer_WithTheExceptionAsTheReason()
    {
        var actions = RecoveryPlaybook.ForShipment(OperationsData.Shipment(exceptions: [OperationsData.Delay()]));

        var action = actions.Should().ContainSingle().Subject;
        action.Problem.Should().Be("Carrier delay");
        action.Action.Should().Contain("2026-11-08");
        action.Rationale.Should().Contain("severe weather");
        action.WhereInConsole.Should().Be("Shipments > RDRN1793717643305");
    }

    [Fact]
    public void SlippedEstimate_WithoutException_SuggestsInformingTheCustomer() =>
        RecoveryPlaybook.ForShipment(OperationsData.Shipment(isDelayed: false))
            .Should().ContainSingle().Which.Problem.Should().Be("Estimate after the promised date");

    [Fact]
    public void OnTimeShipment_NeedsNothing() =>
        RecoveryPlaybook.ForShipment(OperationsData.Shipment(isDelayed: false, estimate: new DateOnly(2026, 11, 6)))
            .Should().ContainSingle().Which.Action.Should().Be("No action needed.");

    [Fact]
    public void FailedOrder_PointsAtStockAndANewOrder() =>
        RecoveryPlaybook.ForOrderWithoutShipment(OperationsData.Order("Failed", "Insufficient inventory: SKU-OOS-001 requested 1, available 0"))
            .Should().ContainSingle().Which.Rationale.Should().Contain("Insufficient inventory");
}
