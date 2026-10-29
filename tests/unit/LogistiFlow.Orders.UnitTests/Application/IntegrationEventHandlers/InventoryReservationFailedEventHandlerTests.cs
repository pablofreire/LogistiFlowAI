using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.Orders.Application.IntegrationEventHandlers;

namespace LogistiFlow.Orders.UnitTests.Application.IntegrationEventHandlers;

public sealed class InventoryReservationFailedEventHandlerTests
{
    [Fact]
    public void DescribeFailure_WithShortages_ListsEachSku()
    {
        var failed = new InventoryReservationFailedEvent(
            Guid.NewGuid(),
            "ORD-1001",
            "Insufficient inventory",
            [new InventoryShortage("SKU-OOS-001", 2, 0), new InventoryShortage("SKU-1007", 9, 8)],
            DateTimeOffset.UnixEpoch);

        InventoryReservationFailedEventHandler.DescribeFailure(failed).Should().Be(
            "Insufficient inventory: SKU-OOS-001 requested 2, available 0; SKU-1007 requested 9, available 8");
    }

    [Fact]
    public void DescribeFailure_WithoutShortages_ReturnsTheReason()
    {
        var failed = new InventoryReservationFailedEvent(Guid.NewGuid(), "ORD-1001", "No single warehouse holds every line", [], DateTimeOffset.UnixEpoch);

        InventoryReservationFailedEventHandler.DescribeFailure(failed).Should().Be("No single warehouse holds every line");
    }
}
