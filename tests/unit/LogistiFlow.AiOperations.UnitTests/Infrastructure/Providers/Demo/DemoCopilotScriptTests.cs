using LogistiFlow.AiOperations.Application.Agent;
using LogistiFlow.AiOperations.Infrastructure.Providers.Demo;
using LogistiFlow.AiOperations.Infrastructure.Providers.Demo.Enums;

namespace LogistiFlow.AiOperations.UnitTests.Infrastructure.Providers.Demo;

public sealed class DemoCopilotScriptTests
{
    public static TheoryData<string, DemoIntent> SampleQuestions => new()
    {
        { "Why is order ORD-1025 delayed?", DemoIntent.ShipmentDelay },
        { "Show shipments at risk of missing their delivery deadline.", DemoIntent.ShipmentsAtRisk },
        { "Which warehouse has the highest number of fulfillment failures?", DemoIntent.WarehouseFailures },
        { "Summarize operational exceptions from the last 24 hours.", DemoIntent.ExceptionsSummary },
        { "What caused the inventory reservation failure for ORD-1025?", DemoIntent.ReservationFailure },
        { "Suggest the next actions for delayed shipments.", DemoIntent.NextActions },
        { "What is the fulfillment status of ORD-7?", DemoIntent.FulfillmentStatus },
        { "Where is ord-7?", DemoIntent.OrderStatus },
        { "Hello", DemoIntent.Unknown },
    };

    [Theory]
    [MemberData(nameof(SampleQuestions))]
    public void Classify_RecognisesEachSupportedQuestion(string question, DemoIntent expected) =>
        DemoCopilotScript.Classify(question).Should().Be(expected);

    [Fact]
    public void EverySampleQuestionOffered_IsRecognised() =>
        CopilotPrompts.SampleQuestions.Select(DemoCopilotScript.Classify).Should().NotContain(DemoIntent.Unknown);

    [Theory]
    [InlineData("Summarize exceptions from the last 6 hours", 6)]
    [InlineData("exceptions in the last 48h", 48)]
    [InlineData("exceptions this week", 168)]
    [InlineData("Summarize exceptions", 24)]
    [InlineData("exceptions in the last 900 hours", 168)]
    public void HoursIn_ReadsTheWindow(string question, int expected) =>
        DemoCopilotScript.HoursIn(question).Should().Be(expected);

    [Fact]
    public void DelayQuestion_WithoutAnyOrderNumber_AsksForOne()
    {
        var step = DemoCopilotScript.Next(["Why is it delayed?"], new Dictionary<string, IReadOnlyList<System.Text.Json.JsonElement>>());

        step.Calls.Should().BeEmpty();
        step.Answer.Should().StartWith("Which order should I check?");
    }

    [Fact]
    public void OrderNumber_FromAnEarlierQuestion_IsReused()
    {
        var step = DemoCopilotScript.Next(["Where is ORD-55?", "Why is it late?"], new Dictionary<string, IReadOnlyList<System.Text.Json.JsonElement>>());

        step.Calls.Should().ContainSingle().Which.Arguments["orderNumber"].Should().Be("ORD-55");
    }
}
