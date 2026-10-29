using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Shared;

public sealed record OrderSummary(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string DestinationCity,
    OrderStatus Status,
    string? StatusReason,
    int LineCount,
    int TotalUnits,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
