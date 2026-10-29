using System.Net;
using System.Net.Http.Json;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.IntegrationTests.Fixtures;

namespace LogistiFlow.IntegrationTests.Shipping;

/// <summary>Operator actions on shipments and their permissions: AC-26.</summary>
public sealed class ShippingApiTests(LogistiFlowEnvironment environment) : IDisposable
{
    private readonly FlowSteps _steps = new(environment);

    [Fact]
    public async Task OpsManager_ResolvesAnOpenException_OtherRolesCannot()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Sandbox Address Not Found");
        var returned = await _steps.WaitForShipmentAsync(orderId, s => FlowSteps.HasStatus(s, "ReturnedToSender"), "the return to sender");
        var exceptionId = FlowSteps.Exceptions(returned, "ReturnedToSender").Single().GetProperty("id").GetGuid();
        var path = new Uri($"/api/shipping/exceptions/{exceptionId}/resolve", UriKind.Relative);
        var body = new { resolution = "Customer reached; address corrected for a new order" };

        using var operatorClient = environment.Shipping.CreateClientAs(LogistiFlowRoles.WarehouseOperator);
        (await operatorClient.PostAsJsonAsync(path, body)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var manager = environment.Shipping.CreateClientAs(LogistiFlowRoles.OpsManager);
        (await manager.PostAsJsonAsync(path, new { resolution = "" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await manager.PostAsJsonAsync(path, body)).StatusCode.Should().Be(HttpStatusCode.OK);

        var again = await manager.PostAsJsonAsync(path, body);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await again.ReadErrorCodeAsync()).Should().Be("shipping.exception_already_resolved");

        var open = await (await _steps.Shipping.GetAsync("/api/shipping/exceptions?status=Open&pageSize=100")).ReadDataAsync();
        open.GetProperty("items").EnumerateArray().Should().NotContain(item => item.GetProperty("id").GetGuid() == exceptionId);
    }

    [Fact]
    public async Task RetryBooking_OnAShipmentThatDidNotFail_IsAConflict()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1);
        var shipment = await _steps.WaitForShipmentAsync(orderId, _ => true, "the shipment to exist");
        using var manager = environment.Shipping.CreateClientAs(LogistiFlowRoles.OpsManager);

        var response = await manager.PostAsync(new Uri($"/api/shipping/shipments/{shipment.GetProperty("id").GetGuid()}/retry-booking", UriKind.Relative), null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorCodeAsync()).Should().Be("shipping.invalid_transition");
    }

    [Fact]
    public async Task Anonymous_IsRejected_AndUnknownShipment_IsNotFound()
    {
        using var anonymous = environment.Shipping.CreateClient();
        (await anonymous.GetAsync("/api/shipping/shipments")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await _steps.Shipping.GetAsync($"/api/shipping/shipments/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose() => _steps.Dispose();
}
