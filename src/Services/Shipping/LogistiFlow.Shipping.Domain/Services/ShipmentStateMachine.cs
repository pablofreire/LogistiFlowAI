using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Domain.Services;

/// <summary>
/// Allowed shipment transitions. Carrier feeds can skip steps (a poll may see "in transit" and "delivered" together),
/// so forward jumps are allowed; going backwards is not, except a new delivery attempt after a failed one.
/// </summary>
public static class ShipmentStateMachine
{
    private static readonly Dictionary<ShipmentStatus, ShipmentStatus[]> Transitions = new()
    {
        [ShipmentStatus.PendingBooking] = [ShipmentStatus.LabelCreated, ShipmentStatus.BookingFailed],
        [ShipmentStatus.BookingFailed] = [ShipmentStatus.PendingBooking],
        [ShipmentStatus.LabelCreated] = [ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery, ShipmentStatus.Delivered],
        [ShipmentStatus.InTransit] = [ShipmentStatus.OutForDelivery, ShipmentStatus.Delivered, ShipmentStatus.ReturnedToSender],
        [ShipmentStatus.OutForDelivery] = [ShipmentStatus.DeliveryAttemptFailed, ShipmentStatus.Delivered],
        [ShipmentStatus.DeliveryAttemptFailed] = [ShipmentStatus.OutForDelivery, ShipmentStatus.InTransit, ShipmentStatus.ReturnedToSender],
        [ShipmentStatus.Delivered] = [],
        [ShipmentStatus.ReturnedToSender] = [],
    };

    public static bool CanTransition(ShipmentStatus from, ShipmentStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Statuses in which the carrier has the package and tracking is polled.</summary>
    public static bool IsTracked(ShipmentStatus status) => status is
        ShipmentStatus.LabelCreated or ShipmentStatus.InTransit or ShipmentStatus.OutForDelivery or ShipmentStatus.DeliveryAttemptFailed;

    public static bool IsTerminal(ShipmentStatus status) => status is ShipmentStatus.Delivered or ShipmentStatus.ReturnedToSender;

    /// <summary>Maps a carrier event code to the shipment status it implies, or <c>null</c> for informational codes.</summary>
    public static ShipmentStatus? StatusForCarrierEvent(string code) => code switch
    {
        "LABEL_CREATED" => ShipmentStatus.LabelCreated,
        "PICKED_UP" or "IN_TRANSIT" or "DELAYED" => ShipmentStatus.InTransit,
        "OUT_FOR_DELIVERY" => ShipmentStatus.OutForDelivery,
        "DELIVERY_ATTEMPT_FAILED" => ShipmentStatus.DeliveryAttemptFailed,
        "DELIVERED" => ShipmentStatus.Delivered,
        "RETURNED_TO_SENDER" => ShipmentStatus.ReturnedToSender,
        _ => null,
    };
}
