using LogistiFlow.Fulfillment.Domain.Entities;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Domain.Services;

namespace LogistiFlow.Fulfillment.UnitTests.Domain.Entities;

public sealed class FulfillmentJobEntityTests
{
    [Fact]
    public void CreateAndStart_ReleasesTheJobForPickingAndRecordsBothSteps()
    {
        var job = NewJob();

        job.Status.Should().Be(FulfillmentJobStatus.Picking);
        job.PendingStatusChanges.Select(change => change.ToStatus)
            .Should().Equal(FulfillmentJobStatus.Created, FulfillmentJobStatus.Picking);
        job.PendingStatusChanges.Should().OnlyContain(change => change.Source == "inventory.inventory-reserved");
    }

    [Fact]
    public void TransitionTo_CancelWhilePicking_Succeeds()
    {
        var job = NewJob();

        job.TransitionTo(FulfillmentJobStatus.Cancelled, "Order cancelled", "orders.order-cancelled", DateTimeOffset.UnixEpoch)
            .IsSuccess.Should().BeTrue();
        job.Status.Should().Be(FulfillmentJobStatus.Cancelled);
    }

    [Fact]
    public void TransitionTo_CancelAfterShipping_IsRejected()
    {
        var job = FulfillmentJobEntity.Restore(1, Guid.NewGuid(), Guid.NewGuid(), "ORD-1", "WH-NJ1", Guid.NewGuid(), FulfillmentJobStatus.Shipped, null, 3, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, []);

        var result = job.TransitionTo(FulfillmentJobStatus.Cancelled, "Too late", "test", DateTimeOffset.UnixEpoch);

        result.Error.Code.Should().Be("fulfillment.invalid_transition");
        job.Status.Should().Be(FulfillmentJobStatus.Shipped);
    }

    [Theory]
    [InlineData(FulfillmentJobStatus.Created, FulfillmentJobStatus.Picking, true)]
    [InlineData(FulfillmentJobStatus.Picking, FulfillmentJobStatus.Packed, true)]
    [InlineData(FulfillmentJobStatus.Packed, FulfillmentJobStatus.Shipped, true)]
    [InlineData(FulfillmentJobStatus.Failed, FulfillmentJobStatus.Picking, true)]
    [InlineData(FulfillmentJobStatus.Created, FulfillmentJobStatus.Shipped, false)]
    [InlineData(FulfillmentJobStatus.Packed, FulfillmentJobStatus.Cancelled, false)]
    [InlineData(FulfillmentJobStatus.Cancelled, FulfillmentJobStatus.Picking, false)]
    public void StateMachine_Transitions(FulfillmentJobStatus from, FulfillmentJobStatus to, bool allowed)
    {
        FulfillmentJobStateMachine.CanTransition(from, to).Should().Be(allowed);
    }

    private static FulfillmentJobEntity NewJob() =>
        FulfillmentJobEntity.CreateAndStart(
            Guid.NewGuid(),
            "ORD-1001",
            "WH-NJ1",
            Guid.NewGuid(),
            [new FulfillmentJobLineEntity("SKU-1001", 2)],
            "inventory.inventory-reserved",
            DateTimeOffset.UnixEpoch);
}
