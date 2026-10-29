using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Domain.Errors;

public static class FulfillmentErrors
{
    public static readonly Error JobNotFound = Error.NotFound("fulfillment.job_not_found", "The fulfillment job does not exist.");

    public static Error InvalidTransition(FulfillmentJobStatus from, FulfillmentJobStatus to) => Error.Conflict(
        "fulfillment.invalid_transition",
        $"A fulfillment job in status {from} cannot move to {to}.");
}
