using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Domain.Services;

/// <summary>Allowed fulfillment job transitions. <c>Failed</c> can return to <c>Picking</c> when a job is reprocessed.</summary>
public static class FulfillmentJobStateMachine
{
    private static readonly Dictionary<FulfillmentJobStatus, FulfillmentJobStatus[]> Transitions = new()
    {
        [FulfillmentJobStatus.Created] = [FulfillmentJobStatus.Picking, FulfillmentJobStatus.Cancelled],
        [FulfillmentJobStatus.Picking] = [FulfillmentJobStatus.Packed, FulfillmentJobStatus.Failed, FulfillmentJobStatus.Cancelled],
        [FulfillmentJobStatus.Packed] = [FulfillmentJobStatus.Shipped, FulfillmentJobStatus.Failed],
        [FulfillmentJobStatus.Failed] = [FulfillmentJobStatus.Picking, FulfillmentJobStatus.Cancelled],
        [FulfillmentJobStatus.Shipped] = [],
        [FulfillmentJobStatus.Cancelled] = [],
    };

    public static bool CanTransition(FulfillmentJobStatus from, FulfillmentJobStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);
}
