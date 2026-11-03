using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.UnitTests.Fixtures;

/// <summary>Service responses as the copilot's clients would return them.</summary>
internal static class OperationsData
{
    public static readonly Guid OrderId = Guid.Parse("01900000-0000-7000-8000-000000001025");

    public static readonly Guid ShipmentId = Guid.Parse("01900000-0000-7000-8000-000000002025");

    public static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    public static OrderView Order(string status = "Shipped", string? reason = "Shipped from WH-CA1", params OrderLineView[] lines) => new(
        OrderId,
        "ORD-1025",
        "Demo Retail",
        "Sandbox Delay",
        "US",
        new DateOnly(2026, 11, 6),
        status,
        reason,
        lines.Length == 0 ? [new OrderLineView(1, "SKU-1003", 1)] : lines,
        Now.AddHours(-3),
        Now);

    public static IReadOnlyList<StatusChangeView> History() =>
    [
        new(null, "Created", "Order received", "api", Now.AddHours(-3)),
        new("PendingInventory", "Reserved", "Inventory reserved in WH-CA1", "inventory.inventory-reserved", Now.AddHours(-3)),
    ];

    public static ShipmentExceptionView Delay(string status = "Open") => new(
        Guid.NewGuid(),
        "ORD-1025",
        "RDRN",
        "RDRN1793717643305",
        "Delay",
        status,
        "Carrier reported a delay: severe weather at the sort facility",
        Now.AddHours(-1),
        status == "Open" ? null : Now);

    public static ShipmentView Shipment(
        string status = "InTransit",
        bool isDelayed = true,
        DateOnly? promised = null,
        DateOnly? estimate = null,
        DateTimeOffset? deliveredAt = null,
        params ShipmentExceptionView[] exceptions) => new(
        ShipmentId,
        "ORD-1025",
        "WH-CA1",
        "RDRN",
        "Roadrunner Ground",
        "RDRN1793717643305",
        status,
        null,
        "Sandbox Delay",
        "US",
        promised ?? new DateOnly(2026, 11, 6),
        new DateOnly(2026, 11, 5),
        estimate ?? new DateOnly(2026, 11, 8),
        deliveredAt,
        1,
        isDelayed,
        [new TrackingEventView("DELAYED", "Delayed: severe weather", "Memphis, US", Now.AddHours(-1), estimate ?? new DateOnly(2026, 11, 8))],
        exceptions);

    public static ShipmentListItemView ListItem(string orderNumber, string status, bool isDelayed, DateOnly? promised, DateOnly? estimate) => new(
        Guid.NewGuid(),
        orderNumber,
        "RDRN",
        "Roadrunner Ground",
        "RDRN" + orderNumber[4..],
        status,
        "Austin",
        "US",
        promised,
        estimate,
        isDelayed,
        isDelayed ? 1 : 0);
}
