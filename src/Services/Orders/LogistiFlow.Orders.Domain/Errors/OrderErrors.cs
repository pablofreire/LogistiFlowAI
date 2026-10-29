using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Domain.Errors;

public static class OrderErrors
{
    public static readonly Error NotFound = Error.NotFound("order.not_found", "The order does not exist.");

    public static readonly Error IdempotencyKeyReused = Error.Conflict(
        "order.idempotency_key_reused",
        "The Idempotency-Key was already used with a different request body.");

    public static readonly Error ConcurrentModification = Error.Conflict(
        "order.concurrent_modification",
        "The order was modified by another operation. Reload it and try again.");

    public static Error InvalidTransition(OrderStatus from, OrderStatus to) => Error.Conflict(
        "order.invalid_transition",
        $"An order in status {from} cannot move to {to}.");
}
