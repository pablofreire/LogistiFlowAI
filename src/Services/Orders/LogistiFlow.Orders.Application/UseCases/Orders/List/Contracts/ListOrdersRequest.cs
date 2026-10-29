using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.UseCases.Orders.List.Contracts;

/// <summary>Paged order list. <c>Search</c> matches the order number or the customer name.</summary>
public sealed record ListOrdersRequest(int? Page, int? PageSize, OrderStatus? Status, string? Search);
