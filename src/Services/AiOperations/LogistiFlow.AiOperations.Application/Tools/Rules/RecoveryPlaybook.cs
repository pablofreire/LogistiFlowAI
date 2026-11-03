using System.Globalization;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Application.Tools.Results;

namespace LogistiFlow.AiOperations.Application.Tools.Rules;

/// <summary>
/// A fixed playbook from problem to next step. It only suggests: every action is one an authorised operator takes
/// in the console, and the copilot executes none of them.
/// </summary>
public static class RecoveryPlaybook
{
    public static IReadOnlyList<RecoveryAction> ForShipment(ShipmentView shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        var where = string.Create(CultureInfo.InvariantCulture, $"Shipments > {shipment.TrackingNumber ?? shipment.OrderNumber}");
        var actions = shipment.Exceptions
            .Where(exception => exception.Status == ShipmentRiskRules.OpenStatus)
            .Select(exception => ForException(shipment, exception, where))
            .ToList();

        if (actions.Count > 0)
        {
            return actions;
        }

        var daysLate = ShipmentRiskRules.DaysLateVersusPromise(shipment.PromisedDeliveryDate, shipment.EstimatedDelivery, shipment.DeliveredAt);
        if (!ShipmentRiskRules.IsFinal(shipment.Status) && daysLate > 0)
        {
            return
            [
                new RecoveryAction(
                    shipment.OrderNumber,
                    "Estimate after the promised date",
                    string.Create(CultureInfo.InvariantCulture, $"Tell the customer the new estimate ({shipment.EstimatedDelivery:yyyy-MM-dd}) before the promised date passes."),
                    string.Create(CultureInfo.InvariantCulture, $"The carrier estimate is {daysLate} day(s) after the promise and no exception is open yet."),
                    where),
            ];
        }

        return [new RecoveryAction(shipment.OrderNumber, "None", "No action needed.", "No open exception and the estimate meets the promised date.", where)];
    }

    /// <summary>Next steps for an order that never shipped, for example after an inventory shortage.</summary>
    public static IReadOnlyList<RecoveryAction> ForOrderWithoutShipment(OrderView order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var where = string.Create(CultureInfo.InvariantCulture, $"Orders > {order.OrderNumber}");
        return order.Status switch
        {
            "Failed" => [new RecoveryAction(order.OrderNumber, "Order failed", "Check stock for the order's SKUs in Inventory; once restocked, create a new order for the customer.", order.StatusReason ?? "The order failed before shipping.", where)],
            "Cancelled" => [new RecoveryAction(order.OrderNumber, "Order cancelled", "No action needed.", order.StatusReason ?? "The order was cancelled.", where)],
            _ => [new RecoveryAction(order.OrderNumber, "Not shipped yet", "Follow the fulfillment job in the warehouse queue.", string.Create(CultureInfo.InvariantCulture, $"The order is {order.Status}; no shipment exists yet."), "Fulfillment")],
        };
    }

    private static RecoveryAction ForException(ShipmentView shipment, ShipmentExceptionView exception, string where) => exception.Type switch
    {
        "Delay" => new RecoveryAction(
            shipment.OrderNumber,
            "Carrier delay",
            string.Create(CultureInfo.InvariantCulture, $"Tell the customer the new estimate ({shipment.EstimatedDelivery:yyyy-MM-dd}); resolve the exception once they are informed."),
            exception.Details,
            where),
        "CarrierUnavailable" => new RecoveryAction(
            shipment.OrderNumber,
            "Carrier unavailable",
            "Booking is retried automatically with backoff; if it keeps failing, an ops manager can use Retry booking.",
            exception.Details,
            where),
        "DeliveryAttemptFailed" => new RecoveryAction(
            shipment.OrderNumber,
            "Delivery attempt failed",
            "Confirm the address and the recipient's availability with the customer; the carrier will attempt again.",
            exception.Details,
            where),
        "ReturnedToSender" => new RecoveryAction(
            shipment.OrderNumber,
            "Returned to sender",
            "Confirm the address with the customer and create a replacement order; the original order is failed.",
            exception.Details,
            where),
        _ => new RecoveryAction(shipment.OrderNumber, exception.Type, "Review the exception on the shipment page.", exception.Details, where),
    };
}
