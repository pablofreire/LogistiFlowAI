using System.Net;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.IntegrationTests.Fixtures;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Flows;

/// <summary>
/// The first business flow across three services, a real broker and three databases:
/// Create Order → Inventory Reservation → Event Publication → Fulfillment Initiated → metrics updated.
/// </summary>
public sealed class OrderToFulfillmentFlowTests(LogistiFlowEnvironment environment)
{
    [Fact]
    public async Task OrderWithStock_IsReservedAndReachesPicking_WithOneCorrelationIdAcrossServices()
    {
        using var orders = environment.Orders.CreateClientAs(LogistiFlowRoles.OpsManager);
        using var inventory = environment.Inventory.CreateClientAs(LogistiFlowRoles.Viewer);
        using var fulfillment = environment.Fulfillment.CreateClientAs(LogistiFlowRoles.Viewer);
        var correlationId = $"flow-{Guid.NewGuid():N}";

        var created = await orders.PostOrderAsync(ApiJson.OrderBody("SKU-1002", 1), $"flow-{Guid.NewGuid():N}", correlationId);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = (await created.ReadDataAsync()).GetProperty("id").GetGuid();

        var order = await EventuallyAsync(
            async () => await (await orders.GetAsync($"/api/orders/{orderId}")).ReadDataAsync(),
            data => data.GetProperty("status").GetString() == "Picking",
            "the order to reach Picking");
        order.GetProperty("statusReason").GetString().Should().StartWith("Fulfillment started in WH-");

        var reservation = await (await inventory.GetAsync($"/api/inventory/reservations/by-order/{orderId}")).ReadDataAsync();
        reservation.GetProperty("status").GetString().Should().Be("Reserved");

        var job = await (await fulfillment.GetAsync($"/api/fulfillment/jobs/by-order/{orderId}")).ReadDataAsync();
        job.GetProperty("status").GetString().Should().Be("Picking");
        job.GetProperty("warehouseCode").GetString().Should().Be(reservation.GetProperty("warehouseCode").GetString());

        var history = await (await orders.GetAsync($"/api/orders/{orderId}/history")).ReadDataAsync();
        history.EnumerateArray().Select(change => change.GetProperty("toStatus").GetString())
            .Should().Equal("Created", "PendingInventory", "Reserved", "Picking");

        // The correlation id of the HTTP request travels with every event of the flow.
        await using var inventoryDb = await environment.OpenAsync(Databases.Inventory);
        await using var fulfillmentDb = await environment.OpenAsync(Databases.Fulfillment);
        (await CountAsync(inventoryDb, "SELECT count(*) FROM messaging.outbox_messages WHERE correlation_id = @correlationId", new { correlationId })).Should().Be(1);
        (await CountAsync(fulfillmentDb, "SELECT count(*) FROM messaging.outbox_messages WHERE correlation_id = @correlationId", new { correlationId })).Should().Be(1);
    }

    [Fact]
    public async Task OrderWithoutStock_FailsWithTheShortageAndStartsNoFulfillment()
    {
        using var orders = environment.Orders.CreateClientAs(LogistiFlowRoles.OpsManager);
        using var fulfillment = environment.Fulfillment.CreateClientAs(LogistiFlowRoles.Viewer);

        var created = await orders.PostOrderAsync(ApiJson.OrderBody("SKU-OOS-001", 1), $"oos-{Guid.NewGuid():N}");
        var orderId = (await created.ReadDataAsync()).GetProperty("id").GetGuid();

        var order = await EventuallyAsync(
            async () => await (await orders.GetAsync($"/api/orders/{orderId}")).ReadDataAsync(),
            data => data.GetProperty("status").GetString() == "Failed",
            "the order to fail");

        order.GetProperty("statusReason").GetString().Should().Be("Insufficient inventory: SKU-OOS-001 requested 1, available 0");
        (await fulfillment.GetAsync($"/api/fulfillment/jobs/by-order/{orderId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Metrics_ReflectTheProcessedOrders()
    {
        using var orders = environment.Orders.CreateClientAs(LogistiFlowRoles.OpsManager);
        using var fulfillment = environment.Fulfillment.CreateClientAs(LogistiFlowRoles.Viewer);
        using var inventory = environment.Inventory.CreateClientAs(LogistiFlowRoles.Viewer);
        var before = await ReadAsync(fulfillment, "/api/metrics/fulfillment");

        var created = await orders.PostOrderAsync(ApiJson.OrderBody("SKU-1003", 2), $"metrics-{Guid.NewGuid():N}");
        var orderId = (await created.ReadDataAsync()).GetProperty("id").GetGuid();
        await EventuallyAsync(
            async () => await (await orders.GetAsync($"/api/orders/{orderId}")).ReadDataAsync(),
            data => data.GetProperty("status").GetString() == "Picking",
            "the order to reach Picking");

        var after = await ReadAsync(fulfillment, "/api/metrics/fulfillment");
        after.GetProperty("totalJobs").GetInt64().Should().Be(before.GetProperty("totalJobs").GetInt64() + 1);

        var orderMetrics = await ReadAsync(orders, "/api/metrics/orders");
        orderMetrics.GetProperty("byStatus").GetProperty("Picking").GetInt64().Should().BeGreaterThan(0);
        orderMetrics.GetProperty("recentActivity").EnumerateArray().Should().Contain(activity => activity.GetProperty("orderId").GetGuid() == orderId);

        var inventoryMetrics = await ReadAsync(inventory, "/api/metrics/inventory");
        inventoryMetrics.GetProperty("totalReserved").GetInt64().Should().BeGreaterThan(0);
    }

    private static async Task<JsonElement> ReadAsync(HttpClient client, string path) => await (await client.GetAsync(path)).ReadDataAsync();
}
