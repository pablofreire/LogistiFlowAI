using LogistiFlow.BuildingBlocks.Domain.Entities;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Domain.Errors;
using LogistiFlow.Fulfillment.Domain.Services;

namespace LogistiFlow.Fulfillment.Domain.Entities;

/// <summary>
/// Warehouse work for one reserved order: pick, pack and ship. One job per order (unique <c>order_id</c>).
/// </summary>
public sealed class FulfillmentJobEntity : Entity
{
    private readonly List<FulfillmentJobLineEntity> _lines;
    private readonly List<FulfillmentJobStatusChangeEntity> _pendingStatusChanges = [];

    private FulfillmentJobEntity(Guid orderId, string orderNumber, string warehouseCode, Guid reservationId, List<FulfillmentJobLineEntity> lines)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        WarehouseCode = warehouseCode;
        ReservationId = reservationId;
        _lines = lines;
    }

    public Guid OrderId { get; }

    public string OrderNumber { get; }

    public string WarehouseCode { get; }

    /// <summary>Public id of the inventory reservation that triggered the job.</summary>
    public Guid ReservationId { get; }

    public FulfillmentJobStatus Status { get; private set; }

    public string? StatusReason { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyList<FulfillmentJobLineEntity> Lines => _lines;

    public IReadOnlyList<FulfillmentJobStatusChangeEntity> PendingStatusChanges => _pendingStatusChanges;

    /// <summary>Creates the job and immediately releases it to the floor for picking.</summary>
    public static FulfillmentJobEntity CreateAndStart(
        Guid orderId,
        string orderNumber,
        string warehouseCode,
        Guid reservationId,
        IEnumerable<FulfillmentJobLineEntity> lines,
        string source,
        DateTimeOffset now)
    {
        var job = new FulfillmentJobEntity(orderId, orderNumber, warehouseCode, reservationId, lines.ToList());
        job.Initialize(now);
        job.Version = 1;
        job.Status = FulfillmentJobStatus.Created;
        job._pendingStatusChanges.Add(new FulfillmentJobStatusChangeEntity(null, FulfillmentJobStatus.Created, "Inventory reserved", source, now));
        job.Apply(FulfillmentJobStatus.Picking, $"Released to the {warehouseCode} floor", source, now);
        return job;
    }

    public static FulfillmentJobEntity Restore(
        long id,
        Guid publicId,
        Guid orderId,
        string orderNumber,
        string warehouseCode,
        Guid reservationId,
        FulfillmentJobStatus status,
        string? statusReason,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<FulfillmentJobLineEntity> lines)
    {
        var job = new FulfillmentJobEntity(orderId, orderNumber, warehouseCode, reservationId, lines.ToList())
        {
            Status = status,
            StatusReason = statusReason,
            Version = version,
        };
        job.Restore(id, publicId, isActive: true, createdAt, updatedAt);
        return job;
    }

    public Result TransitionTo(FulfillmentJobStatus target, string? reason, string source, DateTimeOffset now)
    {
        if (!FulfillmentJobStateMachine.CanTransition(Status, target))
        {
            return FulfillmentErrors.InvalidTransition(Status, target);
        }

        Apply(target, reason, source, now);
        return Result.Success();
    }

    /// <summary>Performs an operator action; the action decides the target status and the state machine guards it.</summary>
    public Result Perform(FulfillmentJobAction action, string? reason, string source, DateTimeOffset now)
    {
        var (target, defaultReason) = action switch
        {
            FulfillmentJobAction.Pack => (FulfillmentJobStatus.Packed, "Packed"),
            FulfillmentJobAction.Ship => (FulfillmentJobStatus.Shipped, $"Shipped from {WarehouseCode}"),
            FulfillmentJobAction.ReportIssue => (FulfillmentJobStatus.Failed, "Issue reported"),
            FulfillmentJobAction.Reprocess => (FulfillmentJobStatus.Picking, "Reprocessing after a failure"),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown fulfillment action."),
        };

        return TransitionTo(target, string.IsNullOrWhiteSpace(reason) ? defaultReason : reason.Trim(), source, now);
    }

    public void MarkPersisted(int storedVersion)
    {
        Version = storedVersion;
        _pendingStatusChanges.Clear();
    }

    private void Apply(FulfillmentJobStatus target, string? reason, string source, DateTimeOffset now)
    {
        _pendingStatusChanges.Add(new FulfillmentJobStatusChangeEntity(Status, target, reason, source, now));
        Status = target;
        StatusReason = reason;
        Touch(now);
    }
}
