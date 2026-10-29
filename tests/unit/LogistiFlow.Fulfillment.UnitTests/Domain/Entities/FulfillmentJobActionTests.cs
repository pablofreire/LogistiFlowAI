using LogistiFlow.Fulfillment.Domain.Entities;
using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.UnitTests.Domain.Entities;

public sealed class FulfillmentJobActionTests
{
    [Fact]
    public void PackThenShip_FollowsTheFloorAndRecordsTheOperator()
    {
        var job = Job(FulfillmentJobStatus.Picking);

        job.Perform(FulfillmentJobAction.Pack, null, "operator:ana@example.com", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        job.Perform(FulfillmentJobAction.Ship, null, "operator:ana@example.com", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();

        job.Status.Should().Be(FulfillmentJobStatus.Shipped);
        job.PendingStatusChanges.Select(change => (change.FromStatus, change.ToStatus)).Should().Equal(
            (FulfillmentJobStatus.Picking, FulfillmentJobStatus.Packed),
            (FulfillmentJobStatus.Packed, FulfillmentJobStatus.Shipped));
        job.PendingStatusChanges.Should().OnlyContain(change => change.Source == "operator:ana@example.com");
        job.StatusReason.Should().Be("Shipped from WH-NJ1");
    }

    [Theory]
    [InlineData(FulfillmentJobStatus.Picking, FulfillmentJobAction.Ship)]
    [InlineData(FulfillmentJobStatus.Packed, FulfillmentJobAction.Pack)]
    [InlineData(FulfillmentJobStatus.Shipped, FulfillmentJobAction.ReportIssue)]
    [InlineData(FulfillmentJobStatus.Picking, FulfillmentJobAction.Reprocess)]
    [InlineData(FulfillmentJobStatus.Cancelled, FulfillmentJobAction.Pack)]
    public void Perform_ActionNotAllowedInCurrentStatus_IsRejectedAndChangesNothing(FulfillmentJobStatus status, FulfillmentJobAction action)
    {
        var job = Job(status);

        var result = job.Perform(action, "reason", "operator:test", DateTimeOffset.UnixEpoch);

        result.Error.Code.Should().Be("fulfillment.invalid_transition");
        job.Status.Should().Be(status);
        job.PendingStatusChanges.Should().BeEmpty();
    }

    [Fact]
    public void ReportIssueThenReprocess_ReturnsTheJobToPickingWithTheHistory()
    {
        var job = Job(FulfillmentJobStatus.Packed);

        job.Perform(FulfillmentJobAction.ReportIssue, "  Damaged carton  ", "operator:test", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        job.Status.Should().Be(FulfillmentJobStatus.Failed);
        job.StatusReason.Should().Be("Damaged carton");

        job.Perform(FulfillmentJobAction.Reprocess, null, "operator:test", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();

        job.Status.Should().Be(FulfillmentJobStatus.Picking);
        job.PendingStatusChanges.Select(change => change.ToStatus).Should().Equal(FulfillmentJobStatus.Failed, FulfillmentJobStatus.Picking);
    }

    private static FulfillmentJobEntity Job(FulfillmentJobStatus status) =>
        FulfillmentJobEntity.Restore(
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ORD-1001",
            "WH-NJ1",
            Guid.NewGuid(),
            status,
            null,
            2,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            [new FulfillmentJobLineEntity("SKU-1001", 2)]);
}
