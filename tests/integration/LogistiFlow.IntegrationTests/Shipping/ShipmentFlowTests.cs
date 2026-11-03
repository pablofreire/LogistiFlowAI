using System.Text.Json;
using Dapper;
using LogistiFlow.BuildingBlocks.Messaging.Consuming;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Shipping;

/// <summary>Ship → book carrier → track → deliver, across five services and the carrier simulator: AC-20, AC-21, AC-22, AC-27.</summary>
public sealed class ShipmentFlowTests(LogistiFlowEnvironment environment) : IDisposable
{
    private const string OnHandOf = """
        SELECT sp.on_hand FROM inventory.stock_positions sp
        JOIN inventory.products p ON p.id = sp.product_id JOIN inventory.warehouses w ON w.id = sp.warehouse_id
        WHERE p.sku = @Sku AND w.code = @Warehouse
        """;

    private const string ReservedOf = """
        SELECT sp.reserved FROM inventory.stock_positions sp
        JOIN inventory.products p ON p.id = sp.product_id JOIN inventory.warehouses w ON w.id = sp.warehouse_id
        WHERE p.sku = @Sku AND w.code = @Warehouse
        """;

    private readonly FlowSteps _steps = new(environment);

    [Fact]
    public async Task NormalScenario_IsBookedTrackedAndDelivered_AndTheOrderFollows()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 2);

        var shipment = await _steps.WaitForShipmentAsync(orderId, s => FlowSteps.HasStatus(s, "Delivered"), "the shipment to be delivered");

        shipment.GetProperty("carrierCode").GetString().Should().Be("RDRN");
        shipment.GetProperty("carrierName").GetString().Should().Be("Roadrunner Ground");
        shipment.GetProperty("trackingNumber").GetString().Should().StartWith("RDRN");
        shipment.GetProperty("estimatedDelivery").GetString().Should().NotBeNullOrEmpty();
        shipment.GetProperty("trackingEvents").EnumerateArray().Select(e => e.GetProperty("code").GetString())
            .Should().Equal("LABEL_CREATED", "PICKED_UP", "IN_TRANSIT", "OUT_FOR_DELIVERY", "DELIVERED");
        shipment.GetProperty("exceptions").GetArrayLength().Should().Be(0);

        var order = await _steps.WaitForOrderAsync(orderId, "Delivered");
        order.GetProperty("statusReason").GetString().Should().StartWith("Delivered by RDRN");

        // Polling continued after delivery would have duplicated events; the unique carrier event id prevents it.
        await using var db = await environment.OpenAsync(Databases.Shipping);
        (await CountAsync(db, "SELECT count(*) FROM shipping.tracking_events t JOIN shipping.shipments s ON s.id = t.shipment_id WHERE s.order_id = @orderId", new { orderId }))
            .Should().Be(5);
    }

    [Fact]
    public async Task ShippedOrder_ConsumesReservedStockOnce()
    {
        var orderId = await _steps.CreateOrderAsync("SKU-1006", 3);
        var jobId = await _steps.JobIdAsync(orderId);
        var job = await (await _steps.Floor.GetAsync($"/api/fulfillment/jobs/{jobId}")).ReadDataAsync();
        var warehouse = job.GetProperty("warehouseCode").GetString()!;
        var parameters = new { Sku = "SKU-1006", Warehouse = warehouse };

        await using var db = await environment.OpenAsync(Databases.Inventory);
        var onHandBefore = await CountAsync(db, OnHandOf, parameters);
        var reservedBefore = await CountAsync(db, ReservedOf, parameters);

        await _steps.ActAsync(jobId, "pack");
        await _steps.ActAsync(jobId, "ship");
        await EventuallyAsync(
            () => db.QuerySingleAsync<string>("SELECT status FROM inventory.reservations WHERE order_id = @orderId", new { orderId }),
            status => status == "Consumed",
            "the reservation to be consumed");

        (await CountAsync(db, OnHandOf, parameters)).Should().Be(onHandBefore - 3);
        (await CountAsync(db, ReservedOf, parameters)).Should().Be(reservedBefore - 3);

        // The same fact delivered again under a new event id gets past the inbox; the reservation status stops it.
        var dispatcher = environment.Inventory.Services.GetRequiredService<IntegrationEventDispatcher>();
        var reservationId = job.GetProperty("reservationId").GetGuid();
        var orderNumber = job.GetProperty("orderNumber").GetString()!;
        await dispatcher.DispatchAsync(Shipped(jobId, orderId, orderNumber, warehouse, reservationId), 1, CancellationToken.None);

        (await CountAsync(db, OnHandOf, parameters)).Should().Be(onHandBefore - 3);
        (await CountAsync(db, "SELECT count(*) FROM inventory.stock_movements WHERE reference_id = @orderNumber AND movement_type = 'Shipment'", new { orderNumber }))
            .Should().Be(1);
    }

    [Fact]
    public async Task InternationalOrder_GoesWithGlobalPost_AndUrgentDomesticWithSimExpress()
    {
        var international = await _steps.ShipOrderAsync("SKU-1003", 1, city: "Toronto", country: "CA");
        var urgent = await _steps.ShipOrderAsync("SKU-1003", 1, promised: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

        (await _steps.WaitForShipmentAsync(international, s => s.GetProperty("trackingNumber").ValueKind == JsonValueKind.String, "the international booking"))
            .GetProperty("carrierCode").GetString().Should().Be("GLBL");
        (await _steps.WaitForShipmentAsync(urgent, s => s.GetProperty("trackingNumber").ValueKind == JsonValueKind.String, "the urgent booking"))
            .GetProperty("carrierCode").GetString().Should().Be("SIMX");
    }

    [Fact]
    public async Task Metrics_CountDeliveredShipments()
    {
        using var shipping = environment.Shipping.CreateClientAs(LogistiFlowRoles.Viewer);
        var before = (await (await shipping.GetAsync("/api/metrics/shipping")).ReadDataAsync()).GetProperty("delivered").GetInt64();

        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1);
        await _steps.WaitForShipmentAsync(orderId, s => FlowSteps.HasStatus(s, "Delivered"), "the shipment to be delivered");

        var metrics = await (await shipping.GetAsync("/api/metrics/shipping")).ReadDataAsync();
        metrics.GetProperty("delivered").GetInt64().Should().BeGreaterThanOrEqualTo(before + 1);
        metrics.GetProperty("byStatus").GetProperty("Delivered").GetInt64().Should().Be(metrics.GetProperty("delivered").GetInt64());
        metrics.GetProperty("openExceptionsByType").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo("CarrierUnavailable", "Delay", "DeliveryAttemptFailed", "ReturnedToSender");
    }

    [Fact]
    public async Task OrderMetrics_ReportProcessingTimes_OnceAnOrderIsDelivered()
    {
        var orderId = await _steps.ShipOrderAsync("SKU-1003", 1);
        await _steps.WaitForOrderAsync(orderId, "Delivered");

        var metrics = await (await _steps.Orders.GetAsync("/api/metrics/orders")).ReadDataAsync();
        var toPicking = metrics.GetProperty("averageSecondsToPicking").GetDouble();
        var toShipped = metrics.GetProperty("averageSecondsToShipped").GetDouble();
        var toDelivered = metrics.GetProperty("averageSecondsToDelivered").GetDouble();

        toPicking.Should().BeGreaterThan(0);
        toShipped.Should().BeGreaterThan(0);
        toDelivered.Should().BeGreaterThan(0);

        // Measured from this order's own history: delivery comes after shipping, which comes after picking.
        await using var db = await environment.OpenAsync(Databases.Orders);
        var reached = (await db.QueryAsync<string>(
                "SELECT c.to_status FROM orders.order_status_changes c JOIN orders.orders o ON o.id = c.order_id WHERE o.public_id = @orderId ORDER BY c.id",
                new { orderId }))
            .ToList();
        reached.Should().ContainInOrder("Picking", "Shipped", "Delivered");
    }

    public void Dispose() => _steps.Dispose();

    private static MessageEnvelope Shipped(Guid jobId, Guid orderId, string orderNumber, string warehouse, Guid reservationId)
    {
        var payload = new FulfillmentShippedEvent(jobId, orderId, orderNumber, warehouse, reservationId, [new OrderLineItem("SKU-1006", 3)], DateTimeOffset.UtcNow);
        return new MessageEnvelope(
            Guid.CreateVersion7(),
            FulfillmentShippedEvent.EventType,
            FulfillmentShippedEvent.Version,
            DateTimeOffset.UtcNow,
            $"test-{orderNumber}",
            null,
            JsonSerializer.SerializeToElement(payload, MessagingJson.Options));
    }
}
