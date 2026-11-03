namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>An order as the gateway returns it.</summary>
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
