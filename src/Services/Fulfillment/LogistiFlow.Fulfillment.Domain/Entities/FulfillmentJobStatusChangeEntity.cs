using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Domain.Entities;

/// <summary>One entry of the job's execution history. Append-only.</summary>
public sealed record FulfillmentJobStatusChangeEntity(
    FulfillmentJobStatus? FromStatus,
    FulfillmentJobStatus ToStatus,
    string? Reason,
    string Source,
    DateTimeOffset OccurredAt);
