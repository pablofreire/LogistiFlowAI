using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Contracts;

/// <param name="JobId">Public id of the fulfillment job.</param>
/// <param name="Action">What the operator did.</param>
/// <param name="Reason">Required when reporting an issue; optional otherwise.</param>
/// <param name="PerformedBy">The operator, recorded in the job history.</param>
public sealed record PerformFulfillmentJobActionRequest(Guid JobId, FulfillmentJobAction Action, string? Reason, string PerformedBy);
