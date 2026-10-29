using LogistiFlow.BuildingBlocks.Application.Messaging.Exceptions;
using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.Orders.Application.Services;

/// <summary>
/// Applies a status change requested by an integration event. Runs inside the dispatcher's transaction.
/// Stale events are ignored; early events and lost optimistic-concurrency races throw a transient exception so
/// the message is retried with backoff.
/// </summary>
public sealed partial class OrderStatusEventApplier(
    IOrderRepository repository,
    TimeProvider timeProvider,
    ILogger<OrderStatusEventApplier> logger)
{
    public async Task ApplyAsync(
        Guid orderId,
        OrderStatus target,
        string reason,
        string source,
        CancellationToken cancellationToken,
        OrderStatus? requiredCurrent = null)
    {
        var order = await repository.GetByPublicIdAsync(orderId, cancellationToken)
            ?? throw new TransientIntegrationEventException($"Order {orderId} is not visible yet.");

        var outcome = order.ApplyEventTransition(target, reason, source, timeProvider.GetUtcNow(), requiredCurrent);
        switch (outcome)
        {
            case EventTransitionOutcome.Applied:
                if (!await repository.TryUpdateStatusAsync(order, cancellationToken))
                {
                    throw new TransientIntegrationEventException($"Order {order.OrderNumber} changed concurrently.");
                }

                LogApplied(logger, order.OrderNumber, target, source);
                break;
            case EventTransitionOutcome.Stale:
                LogStale(logger, order.OrderNumber, order.Status, target, source);
                break;
            default:
                throw new TransientIntegrationEventException(
                    $"Order {order.OrderNumber} is {order.Status}; {source} cannot move it to {target} yet.");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderNumber} moved to {Target} by {Source}")]
    private static partial void LogApplied(ILogger logger, string orderNumber, OrderStatus target, string source);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ignored stale {Source} for order {OrderNumber}: status {Current}, target {Target}")]
    private static partial void LogStale(ILogger logger, string orderNumber, OrderStatus current, OrderStatus target, string source);
}
