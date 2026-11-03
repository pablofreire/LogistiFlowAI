namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>A warehouse job as the Fulfillment service returns it.</summary>
public sealed record FulfillmentJobView(
    Guid Id,
    string OrderNumber,
    string WarehouseCode,
    string Status,
    string? StatusReason,
    IReadOnlyList<StatusChangeView> History,
    DateTimeOffset UpdatedAt);
