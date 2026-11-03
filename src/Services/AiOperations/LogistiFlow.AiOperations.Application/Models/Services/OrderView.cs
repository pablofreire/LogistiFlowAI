namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>An order as the Orders service returns it.</summary>
public sealed record OrderView(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    string Status,
    string? StatusReason,
    IReadOnlyList<OrderLineView> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
