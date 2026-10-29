using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Shared;

public sealed record OrderDetails(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    OrderStatus Status,
    string? StatusReason,
    IReadOnlyList<OrderLineDetails> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
