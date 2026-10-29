using LogistiFlow.Orders.Domain.Entities;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.UnitTests.Domain.Entities;

public sealed class OrderEntityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NewOrder_WaitsForInventoryAndRecordsBothSteps()
    {
        var order = NewOrder();

        order.Status.Should().Be(OrderStatus.PendingInventory);
        order.Version.Should().Be(1);
        order.PublicId.Should().NotBeEmpty();
        order.DestinationCountry.Should().Be("US");
        order.Lines.Select(line => line.LineNumber).Should().Equal(1, 2);
        order.PendingStatusChanges.Select(change => change.ToStatus)
            .Should().Equal(OrderStatus.Created, OrderStatus.PendingInventory);
    }

    [Fact]
    public void Create_WithoutLines_Throws()
    {
        var act = () => OrderEntity.Create("Acme", "Austin", "US", null, [], "key", "hash", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cancel_PendingOrder_Succeeds()
    {
        var order = NewOrder();

        var result = order.Cancel("Customer request", Now);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.StatusReason.Should().Be("Customer request");
    }

    [Fact]
    public void Cancel_OrderInPicking_IsRejectedAsConflict()
    {
        var order = OrderInStatus(OrderStatus.Picking);

        var result = order.Cancel("Too late", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("order.invalid_transition");
        order.Status.Should().Be(OrderStatus.Picking);
    }

    [Fact]
    public void ApplyEventTransition_ExpectedNextStep_IsApplied()
    {
        var order = NewOrder();

        var outcome = order.ApplyEventTransition(OrderStatus.Reserved, "Reserved", "inventory.inventory-reserved", Now);

        outcome.Should().Be(EventTransitionOutcome.Applied);
        order.Status.Should().Be(OrderStatus.Reserved);
    }

    [Fact]
    public void ApplyEventTransition_DuplicateEvent_IsStale()
    {
        var order = OrderInStatus(OrderStatus.Reserved);

        order.ApplyEventTransition(OrderStatus.Reserved, null, "inventory.inventory-reserved", Now)
            .Should().Be(EventTransitionOutcome.Stale);
    }

    [Fact]
    public void ApplyEventTransition_ReservationAfterCancellation_IsStaleAndKeepsCancelled()
    {
        var order = NewOrder();
        order.Cancel("Customer request", Now);

        var outcome = order.ApplyEventTransition(OrderStatus.Reserved, null, "inventory.inventory-reserved", Now);

        outcome.Should().Be(EventTransitionOutcome.Stale);
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void ApplyEventTransition_EventThatOvertookItsPrerequisite_IsNotYetApplicable()
    {
        var order = NewOrder();

        var outcome = order.ApplyEventTransition(OrderStatus.Picking, null, "fulfillment.fulfillment-started", Now);

        outcome.Should().Be(EventTransitionOutcome.NotYetApplicable);
        order.Status.Should().Be(OrderStatus.PendingInventory);
    }

    [Fact]
    public void ApplyEventTransition_OlderEventAfterProgress_IsStale()
    {
        var order = OrderInStatus(OrderStatus.Picking);

        order.ApplyEventTransition(OrderStatus.Reserved, null, "inventory.inventory-reserved", Now)
            .Should().Be(EventTransitionOutcome.Stale);
    }

    [Fact]
    public void MarkPersisted_StoresVersionAndClearsPendingHistory()
    {
        var order = NewOrder();

        order.MarkPersisted(3);

        order.Version.Should().Be(3);
        order.PendingStatusChanges.Should().BeEmpty();
    }

    private static OrderEntity NewOrder() =>
        OrderEntity.Create("Acme Retail", "Austin", "us", null, [("SKU-1", 2), ("SKU-2", 1)], "key-1", "hash-1", Now);

    private static OrderEntity OrderInStatus(OrderStatus status) =>
        OrderEntity.Restore(1, Guid.CreateVersion7(), "ORD-1001", "Acme", "Austin", "US", null, status, null, 2, "key", "hash", true, Now, Now, []);
}
