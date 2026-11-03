using LogistiFlow.McpServer.Contracts.Views;

namespace LogistiFlow.McpServer.UnitTests.Fixtures;

/// <summary>Sample API payloads in the shapes the LogistiFlow services return.</summary>
public static class LogisticsData
{
    public static readonly Guid OrderId = Guid.Parse("6f1c2a4e-0d7b-4c51-9a8e-1b2c3d4e5f60");

    public static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    public static OrderSearchItemView[] Search(string orderNumber = "ORD-1025") => [new(OrderId, orderNumber, "Shipped")];

    public static object SearchPage(string orderNumber = "ORD-1025") => new { items = Search(orderNumber), totalCount = 1 };

    public static OrderView Order(string status = "Shipped") => new(
        OrderId, "ORD-1025", "Acme Retail", "Austin", "US", new DateOnly(2026, 11, 10), status, null,
        [new OrderLineView(1, "SKU-1003", 2)], Now.AddDays(-2), Now.AddHours(-3));

    public static StatusChangeView[] History() =>
    [
        new(null, "Created", null, "orders-api", Now.AddDays(-2)),
        new("Created", "Reserved", null, "inventory", Now.AddDays(-2).AddSeconds(1)),
        new("Reserved", "Picking", null, "fulfillment", Now.AddDays(-2).AddSeconds(2)),
        new("Picking", "Shipped", null, "shipping", Now.AddHours(-3)),
    ];

    public static SkuAvailabilityView Availability() => new("SKU-1003", "Barcode scanner", 42, [new("WH-AUS", 50, 8, 42)]);

    public static ShipmentView Shipment() => new(
        Guid.NewGuid(), "ORD-1025", "WH-AUS", "SIMX", "Simulated Express", "SIMX-0001", "InTransit", null, "Austin", "US",
        new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 8), new DateOnly(2026, 11, 12), null, 1, true,
        [new TrackingEventView("IN_TRANSIT", "Departed facility", "Dallas, TX", Now.AddHours(-2), new DateOnly(2026, 11, 12))],
        [new ShipmentExceptionView(Guid.NewGuid(), "ORD-1025", "SIMX", "SIMX-0001", "Delay", "Open", "Severe weather on the route.", Now.AddHours(-2), null)]);

    public static object ExceptionPage() => new { items = Shipment().Exceptions, totalCount = 7 };

    public static FulfillmentMetricsView FulfillmentMetrics() => new(
        12, new Dictionary<string, long>(StringComparer.Ordinal) { ["Picking"] = 3, ["Failed"] = 2, ["Shipped"] = 7 },
        [new WarehouseJobCountView("WH-AUS", 2, 1, 7), new WarehouseJobCountView("WH-NYC", 1, 1, 5)]);

    public static InventoryMetricsView InventoryMetrics() => new(1200, 3, 10);
}
