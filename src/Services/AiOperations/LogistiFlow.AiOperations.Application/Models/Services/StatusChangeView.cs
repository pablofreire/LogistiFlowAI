namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>A status change of an order or a fulfillment job.</summary>
public sealed record StatusChangeView(
    string? FromStatus,
    string ToStatus,
    string? Reason,
    string Source,
    DateTimeOffset OccurredAt);
