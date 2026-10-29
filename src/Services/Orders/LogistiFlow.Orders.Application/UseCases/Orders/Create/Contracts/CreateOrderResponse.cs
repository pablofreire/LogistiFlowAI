using LogistiFlow.Orders.Application.UseCases.Orders.Shared;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;

/// <summary>The stored order. <c>Replayed</c> is true when the idempotency key matched an earlier identical request.</summary>
public sealed record CreateOrderResponse(OrderDetails Order, bool Replayed);
