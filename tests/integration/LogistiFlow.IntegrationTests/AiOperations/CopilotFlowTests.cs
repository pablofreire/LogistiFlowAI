using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.IntegrationTests.Fixtures;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.AiOperations;

/// <summary>
/// The copilot against the real services (demo provider): answers built from live data, the caller's token passed
/// through, every question audited without the token: AC-32 to AC-38.
/// </summary>
public sealed class CopilotFlowTests(LogistiFlowEnvironment environment) : IDisposable
{
    private readonly FlowSteps _steps = new(environment);
    private readonly HttpClient _manager = environment.AiOperations.CreateClientAs(LogistiFlowRoles.OpsManager);

    [Fact]
    public async Task DelayedOrder_IsExplainedFromTheShipmentData_AndAudited()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Delay");
        await _steps.WaitForShipmentAsync(orderId, shipment => FlowSteps.HasStatus(shipment, "Delivered"), "the delayed shipment to be delivered");
        var orderNumber = (await _steps.WaitForOrderAsync(orderId, "Delivered")).GetProperty("orderNumber").GetString()!;

        var answer = await AskAsync(_manager, $"Why is order {orderNumber} delayed?");

        answer.GetProperty("simulated").GetBoolean().Should().BeTrue();
        answer.GetProperty("provider").GetString().Should().Be("demo");
        var call = answer.GetProperty("toolCalls").EnumerateArray().Should().ContainSingle().Subject;
        call.GetProperty("name").GetString().Should().Be("AnalyzeShipmentDelay");
        call.GetProperty("outcome").GetString().Should().Be("ok");
        answer.GetProperty("answer").GetString().Should()
            .Contain($"{orderNumber}: Delivered")
            .And.Contain("- Delay (Resolved): ")
            .And.Contain("severe weather")
            .And.Contain("Roadrunner Ground");

        await using var db = await environment.OpenAsync(Databases.AiOperations);
        var audited = await db.QuerySingleAsync<(string Question, string Outcome, string UserEmail, bool Simulated)>(
            "SELECT question, outcome, user_email, simulated FROM aiops.copilot_interactions WHERE public_id = @Id",
            new { Id = answer.GetProperty("interactionId").GetGuid() });
        audited.Should().Be(($"Why is order {orderNumber} delayed?", "Answered", "opsmanager@integration.tests", true));
        (await db.ExecuteScalarAsync<long>(
                "SELECT count(*) FROM aiops.copilot_tool_calls t JOIN aiops.copilot_interactions i ON i.id = t.interaction_id WHERE i.public_id = @Id AND t.tool_name = 'AnalyzeShipmentDelay' AND t.outcome = 'Ok'",
                new { Id = answer.GetProperty("interactionId").GetGuid() }))
            .Should().Be(1);
    }

    [Fact]
    public async Task ReservationFailure_IsExplainedFromTheOrderAndTheStock_InTwoToolRounds()
    {
        var orderId = await _steps.CreateOrderAsync("SKU-OOS-001", 1);
        var orderNumber = (await _steps.WaitForOrderAsync(orderId, "Failed")).GetProperty("orderNumber").GetString()!;

        var answer = await AskAsync(_manager, $"What caused the inventory reservation failure for {orderNumber}?");

        answer.GetProperty("toolCalls").EnumerateArray().Select(call => call.GetProperty("name").GetString())
            .Should().Equal("GetOrderDetails", "GetInventoryAvailability");
        answer.GetProperty("answer").GetString().Should()
            .Contain($"{orderNumber} failed: Insufficient inventory")
            .And.Contain("Stock of SKU-OOS-001 now: 0 available");
    }

    [Fact]
    public async Task UnknownOrder_IsReportedNotFound_NotInvented()
    {
        var answer = await AskAsync(_manager, "Why is order ORD-999999999 delayed?");

        answer.GetProperty("toolCalls")[0].GetProperty("outcome").GetString().Should().Be("not_found");
        answer.GetProperty("answer").GetString().Should().StartWith("I could not analyse the shipment: Order ORD-999999999 does not exist.");
    }

    [Fact]
    public async Task FollowUp_InTheSameConversation_ReusesTheOrderNumber()
    {
        var orderId = await _steps.CreateOrderAsync("SKU-1001", 1);
        var orderNumber = (await _steps.WaitForOrderAsync(orderId, "Picking")).GetProperty("orderNumber").GetString()!;

        var first = await AskAsync(_manager, $"What is the fulfillment status of {orderNumber}?");
        first.GetProperty("answer").GetString().Should().Contain($"{orderNumber}: warehouse job Picking");

        var followUp = await AskAsync(_manager, "And why is it late?", first.GetProperty("conversationId").GetGuid());

        followUp.GetProperty("conversationId").GetGuid().Should().Be(first.GetProperty("conversationId").GetGuid());
        followUp.GetProperty("toolCalls")[0].GetProperty("arguments").GetString().Should().Contain(orderNumber);
        followUp.GetProperty("answer").GetString().Should().Contain($"{orderNumber} has no shipment; it is Picking.");
    }

    [Fact]
    public async Task Status_SaysTheDemoProviderIsSimulated()
    {
        var status = await (await _manager.GetAsync("/api/ai/copilot/status")).ReadDataAsync();

        status.GetProperty("simulated").GetBoolean().Should().BeTrue();
        status.GetProperty("tools").GetArrayLength().Should().Be(9);
        status.GetProperty("sampleQuestions").GetArrayLength().Should().Be(6);
    }

    [Fact]
    public async Task Authorization_ViewerAndAnonymousAreRejected_AuditIsForManagers()
    {
        using var viewer = environment.AiOperations.CreateClientAs(LogistiFlowRoles.Viewer);
        using var anonymous = environment.AiOperations.CreateClientAs(null);
        using var floor = environment.AiOperations.CreateClientAs(LogistiFlowRoles.WarehouseOperator);
        var body = new { question = "Which warehouse has the highest number of fulfillment failures?" };

        (await viewer.PostAsJsonAsync("/api/ai/copilot/ask", body)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await anonymous.PostAsJsonAsync("/api/ai/copilot/ask", body)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await floor.PostAsJsonAsync("/api/ai/copilot/ask", body)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await floor.GetAsync("/api/ai/interactions")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var audit = await (await _manager.GetAsync("/api/ai/interactions?pageSize=100")).ReadDataAsync();
        audit.GetProperty("items").EnumerateArray().Should().Contain(item =>
            item.GetProperty("userEmail").GetString() == "warehouseoperator@integration.tests"
            && item.GetProperty("tools").EnumerateArray().Single().GetString() == "GetWarehouseMetrics");
    }

    [Fact]
    public async Task Validation_RejectsAnEmptyOrTooLongQuestion()
    {
        (await _manager.PostAsJsonAsync("/api/ai/copilot/ask", new { question = "" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _manager.PostAsJsonAsync("/api/ai/copilot/ask", new { question = new string('x', 501) })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Audit_NeverStoresTheCallersToken()
    {
        await AskAsync(_manager, "Summarize operational exceptions from the last 24 hours.");

        await using var db = await environment.OpenAsync(Databases.AiOperations);
        var rows = await db.QueryAsync<string>(
            "SELECT row_to_json(i)::text FROM aiops.copilot_interactions i UNION ALL SELECT row_to_json(t)::text FROM aiops.copilot_tool_calls t");
        rows.Should().NotBeEmpty().And.NotContain(row => row.Contains("eyJ", StringComparison.Ordinal) || row.Contains("Bearer", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        _manager.Dispose();
        _steps.Dispose();
    }

    private static async Task<JsonElement> AskAsync(HttpClient client, string question, Guid? conversationId = null)
    {
        using var response = await client.PostAsJsonAsync("/api/ai/copilot/ask", new { question, conversationId });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.ReadDataAsync();
    }
}
