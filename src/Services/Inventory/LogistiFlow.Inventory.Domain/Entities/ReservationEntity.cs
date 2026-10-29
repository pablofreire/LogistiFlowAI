using LogistiFlow.BuildingBlocks.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.Domain.Entities;

/// <summary>
/// The outcome of reserving stock for one order. There is at most one per order (unique <c>order_id</c>), which
/// also makes the reservation idempotent per order on top of the inbox.
/// </summary>
public sealed class ReservationEntity : Entity
{
    private readonly List<ReservationLineEntity> _lines;

    private ReservationEntity(Guid orderId, string orderNumber, string? warehouseCode, ReservationStatus status, string? failureReason, List<ReservationLineEntity> lines)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        WarehouseCode = warehouseCode;
        Status = status;
        FailureReason = failureReason;
        _lines = lines;
    }

    /// <summary>Public identifier of the order in the Orders service. Not a foreign key: different database.</summary>
    public Guid OrderId { get; }

    public string OrderNumber { get; }

    public string? WarehouseCode { get; }

    public ReservationStatus Status { get; private set; }

    public string? FailureReason { get; }

    public IReadOnlyList<ReservationLineEntity> Lines => _lines;

    public static ReservationEntity Reserved(Guid orderId, string orderNumber, string warehouseCode, IEnumerable<ReservationLineEntity> lines, DateTimeOffset now) =>
        Create(orderId, orderNumber, warehouseCode, ReservationStatus.Reserved, null, lines.ToList(), now);

    public static ReservationEntity Failed(Guid orderId, string orderNumber, string reason, DateTimeOffset now) =>
        Create(orderId, orderNumber, null, ReservationStatus.Failed, reason, [], now);

    public static ReservationEntity CancelledBeforeReservation(Guid orderId, string orderNumber, DateTimeOffset now) =>
        Create(orderId, orderNumber, null, ReservationStatus.Cancelled, "Order cancelled before reservation", [], now);

    public static ReservationEntity Restore(
        long id,
        Guid publicId,
        Guid orderId,
        string orderNumber,
        string? warehouseCode,
        ReservationStatus status,
        string? failureReason,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<ReservationLineEntity> lines)
    {
        var reservation = new ReservationEntity(orderId, orderNumber, warehouseCode, status, failureReason, lines.ToList());
        reservation.Restore(id, publicId, isActive: true, createdAt, updatedAt);
        return reservation;
    }

    /// <summary>Returns <c>true</c> when the reservation held stock and now gives it back.</summary>
    public bool Release(DateTimeOffset now)
    {
        if (Status != ReservationStatus.Reserved)
        {
            return false;
        }

        Status = ReservationStatus.Released;
        Touch(now);
        return true;
    }

    /// <summary>Returns <c>true</c> when the reservation held stock and the shipment now takes it.</summary>
    public bool Consume(DateTimeOffset now)
    {
        if (Status != ReservationStatus.Reserved)
        {
            return false;
        }

        Status = ReservationStatus.Consumed;
        Touch(now);
        return true;
    }

    private static ReservationEntity Create(
        Guid orderId,
        string orderNumber,
        string? warehouseCode,
        ReservationStatus status,
        string? failureReason,
        List<ReservationLineEntity> lines,
        DateTimeOffset now)
    {
        var reservation = new ReservationEntity(orderId, orderNumber, warehouseCode, status, failureReason, lines);
        reservation.Initialize(now);
        return reservation;
    }
}
