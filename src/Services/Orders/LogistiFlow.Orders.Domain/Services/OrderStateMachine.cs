using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Domain.Services;

/// <summary>
/// The single source of truth for order status transitions. Every status change, whether requested through the
/// API or caused by an integration event, is checked here.
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Created] = [OrderStatus.PendingInventory, OrderStatus.Cancelled],
        [OrderStatus.PendingInventory] = [OrderStatus.Reserved, OrderStatus.Failed, OrderStatus.Cancelled],
        [OrderStatus.Reserved] = [OrderStatus.Picking, OrderStatus.Cancelled],
        [OrderStatus.Picking] = [OrderStatus.Packed, OrderStatus.Failed],
        [OrderStatus.Packed] = [OrderStatus.Shipped, OrderStatus.Failed],
        [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Failed],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Failed] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Failed;

    public static IReadOnlyList<OrderStatus> AllowedTargets(OrderStatus from) =>
        Transitions.TryGetValue(from, out var targets) ? targets : [];

    /// <summary>
    /// Position on the happy path. Used to recognise stale events: an event whose target is behind the current
    /// position has already been superseded.
    /// </summary>
    public static int ProgressRank(OrderStatus status) => status switch
    {
        OrderStatus.Created => 0,
        OrderStatus.PendingInventory => 1,
        OrderStatus.Reserved => 2,
        OrderStatus.Picking => 3,
        OrderStatus.Packed => 4,
        OrderStatus.Shipped => 5,
        OrderStatus.Delivered => 6,
        _ => int.MaxValue,
    };
}
