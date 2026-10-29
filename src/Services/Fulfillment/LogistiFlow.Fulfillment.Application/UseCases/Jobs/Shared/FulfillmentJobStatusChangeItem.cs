using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;

public sealed record FulfillmentJobStatusChangeItem(
    FulfillmentJobStatus? FromStatus,
    FulfillmentJobStatus ToStatus,
    string? Reason,
    string Source,
    DateTimeOffset OccurredAt);
