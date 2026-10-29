using LogistiFlow.BuildingBlocks.Domain.Entities;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Domain.Errors;
using LogistiFlow.Orders.Domain.Services;

namespace LogistiFlow.Orders.Domain.Entities;

/// <summary>
/// An order received from a merchant. The aggregate guards its status through <see cref="OrderStateMachine"/>
/// and records every change, which the repository persists as the order's history.
/// </summary>
public sealed class OrderEntity : Entity
{
    public const string ApiSource = "api";

    private readonly List<OrderLineEntity> _lines;
    private readonly List<OrderStatusChangeEntity> _pendingStatusChanges = [];

    private OrderEntity(
        string customerName,
        string destinationCity,
        string destinationCountry,
        DateOnly? promisedDeliveryDate,
        string idempotencyKey,
        string requestHash,
        List<OrderLineEntity> lines)
    {
        CustomerName = customerName;
        DestinationCity = destinationCity;
        DestinationCountry = destinationCountry;
        PromisedDeliveryDate = promisedDeliveryDate;
        IdempotencyKey = idempotencyKey;
        RequestHash = requestHash;
        _lines = lines;
    }

    /// <summary>Human-readable number such as <c>ORD-1025</c>, assigned by the database sequence.</summary>
    public string OrderNumber { get; private set; } = string.Empty;

    public string CustomerName { get; }

    public string DestinationCity { get; }

    /// <summary>ISO 3166-1 alpha-2 country code.</summary>
    public string DestinationCountry { get; }

    public DateOnly? PromisedDeliveryDate { get; }

    public OrderStatus Status { get; private set; }

    /// <summary>Why the order is in its current status, for example the inventory shortage behind <c>Failed</c>.</summary>
    public string? StatusReason { get; private set; }

    /// <summary>Optimistic concurrency token; incremented on every persisted change.</summary>
    public int Version { get; private set; }

    public string IdempotencyKey { get; }

    /// <summary>SHA-256 of the canonical request, used to detect a reused idempotency key with a different body.</summary>
    public string RequestHash { get; }

    public IReadOnlyList<OrderLineEntity> Lines => _lines;

    /// <summary>Status changes not yet written to the history table.</summary>
    public IReadOnlyList<OrderStatusChangeEntity> PendingStatusChanges => _pendingStatusChanges;

    public static OrderEntity Create(
        string customerName,
        string destinationCity,
        string destinationCountry,
        DateOnly? promisedDeliveryDate,
        IEnumerable<(string Sku, int Quantity)> lines,
        string idempotencyKey,
        string requestHash,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var orderLines = lines.Select((line, index) => new OrderLineEntity(index + 1, line.Sku, line.Quantity)).ToList();
        if (orderLines.Count == 0)
        {
            throw new ArgumentException("An order needs at least one line.", nameof(lines));
        }

        var order = new OrderEntity(
            customerName,
            destinationCity,
            destinationCountry.ToUpperInvariant(),
            promisedDeliveryDate,
            idempotencyKey,
            requestHash,
            orderLines);

        order.Initialize(now);
        order.Version = 1;
        order.Status = OrderStatus.Created;
        order._pendingStatusChanges.Add(new OrderStatusChangeEntity(null, OrderStatus.Created, "Order received", ApiSource, now));

        // The OrderCreated event is published in the same transaction, so the order immediately waits for inventory.
        order.Apply(OrderStatus.PendingInventory, "Awaiting inventory reservation", ApiSource, now);
        return order;
    }

    public static OrderEntity Restore(
        long id,
        Guid publicId,
        string orderNumber,
        string customerName,
        string destinationCity,
        string destinationCountry,
        DateOnly? promisedDeliveryDate,
        OrderStatus status,
        string? statusReason,
        int version,
        string idempotencyKey,
        string requestHash,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<OrderLineEntity> lines)
    {
        var order = new OrderEntity(customerName, destinationCity, destinationCountry, promisedDeliveryDate, idempotencyKey, requestHash, lines.ToList())
        {
            OrderNumber = orderNumber,
            Status = status,
            StatusReason = statusReason,
            Version = version,
        };
        order.Restore(id, publicId, isActive, createdAt, updatedAt);
        return order;
    }

    public void AssignOrderNumber(string orderNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        OrderNumber = orderNumber;
    }

    /// <summary>A transition requested by a user. Invalid transitions are rejected with a conflict error.</summary>
    public Result TransitionTo(OrderStatus target, string? reason, string source, DateTimeOffset now)
    {
        if (!OrderStateMachine.CanTransition(Status, target))
        {
            return OrderErrors.InvalidTransition(Status, target);
        }

        Apply(target, reason, source, now);
        return Result.Success();
    }

    public Result Cancel(string reason, DateTimeOffset now) => TransitionTo(OrderStatus.Cancelled, reason, ApiSource, now);

    /// <summary>
    /// A transition caused by an integration event. Events can arrive duplicated or out of order, so instead of
    /// failing this distinguishes an event that is already superseded (ignore it) from one that arrived before
    /// its prerequisite (retry it later).
    /// </summary>
    public EventTransitionOutcome ApplyEventTransition(
        OrderStatus target,
        string? reason,
        string source,
        DateTimeOffset now,
        OrderStatus? requiredCurrent = null)
    {
        if (Status == target
            || OrderStateMachine.IsTerminal(Status)
            || OrderStateMachine.ProgressRank(Status) > OrderStateMachine.ProgressRank(target))
        {
            return EventTransitionOutcome.Stale;
        }

        // Off-path targets such as Failed are reachable from several statuses. An event that only makes sense after
        // a given status (a return needs a shipped order) waits for it instead of skipping steps.
        if (requiredCurrent is { } required && Status != required)
        {
            return OrderStateMachine.ProgressRank(Status) < OrderStateMachine.ProgressRank(required)
                ? EventTransitionOutcome.NotYetApplicable
                : EventTransitionOutcome.Stale;
        }

        if (!OrderStateMachine.CanTransition(Status, target))
        {
            return EventTransitionOutcome.NotYetApplicable;
        }

        Apply(target, reason, source, now);
        return EventTransitionOutcome.Applied;
    }

    /// <summary>Called by the repository after the changes are stored, with the version now in the database.</summary>
    public void MarkPersisted(int storedVersion)
    {
        Version = storedVersion;
        _pendingStatusChanges.Clear();
    }

    private void Apply(OrderStatus target, string? reason, string source, DateTimeOffset now)
    {
        _pendingStatusChanges.Add(new OrderStatusChangeEntity(Status, target, reason, source, now));
        Status = target;
        StatusReason = reason;
        Touch(now);
    }
}
