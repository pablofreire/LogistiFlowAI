using System.Net;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.IntegrationTests.Fixtures;

namespace LogistiFlow.IntegrationTests.Fulfillment;

/// <summary>Warehouse floor actions through the real API: AC-17, AC-18 and AC-19.</summary>
public sealed class FulfillmentActionsTests(LogistiFlowEnvironment environment) : IDisposable
{
    private readonly FlowSteps _steps = new(environment);

    [Fact]
    public async Task PackThenShip_MovesTheJobAndTheOrder()
    {
        var orderId = await _steps.CreateOrderAsync("SKU-1003", 1);
        var jobId = await _steps.JobIdAsync(orderId);

        var packed = await _steps.ActAsync(jobId, "pack");
        packed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await packed.ReadDataAsync()).GetProperty("status").GetString().Should().Be("Packed");
        await _steps.WaitForOrderAsync(orderId, "Packed");

        (await _steps.ActAsync(jobId, "ship")).StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await _steps.WaitForOrderAsync(orderId, "Shipped");

        order.GetProperty("statusReason").GetString().Should().StartWith("Shipped from WH-");
    }

    [Fact]
    public async Task ShipBeforePack_IsRejectedWith409()
    {
        var jobId = await _steps.JobIdAsync(await _steps.CreateOrderAsync("SKU-1003", 1));

        var response = await _steps.ActAsync(jobId, "ship");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCodeAsync()).Should().Be("fulfillment.invalid_transition");
    }

    [Fact]
    public async Task Viewer_CannotActOnTheFloor()
    {
        var jobId = await _steps.JobIdAsync(await _steps.CreateOrderAsync("SKU-1003", 1));
        using var viewer = environment.Fulfillment.CreateClientAs(LogistiFlowRoles.Viewer);

        var response = await viewer.PostAsync(new Uri($"/api/fulfillment/jobs/{jobId}/pack", UriKind.Relative), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReportedIssue_CanBeReprocessed_AndTheHistoryShowsBoth()
    {
        var orderId = await _steps.CreateOrderAsync("SKU-1003", 1);
        var jobId = await _steps.JobIdAsync(orderId);

        (await _steps.ActAsync(jobId, "report-issue", new { reason = "x" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _steps.ActAsync(jobId, "report-issue", new { reason = "Damaged carton at bin A-12" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _steps.ActAsync(jobId, "reprocess")).StatusCode.Should().Be(HttpStatusCode.OK);

        var job = await (await _steps.Floor.GetAsync($"/api/fulfillment/jobs/{jobId}")).ReadDataAsync();
        job.GetProperty("status").GetString().Should().Be("Picking");
        job.GetProperty("history").EnumerateArray().Select(change => change.GetProperty("toStatus").GetString())
            .Should().Equal("Created", "Picking", "Failed", "Picking");

        // A job on hold is a warehouse matter: the order itself stays in Picking.
        var order = await (await _steps.Orders.GetAsync($"/api/orders/{orderId}")).ReadDataAsync();
        order.GetProperty("status").GetString().Should().Be("Picking");
    }

    public void Dispose() => _steps.Dispose();
}
