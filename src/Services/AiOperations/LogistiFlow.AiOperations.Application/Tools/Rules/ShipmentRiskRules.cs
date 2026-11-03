using System.Globalization;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Application.Tools.Results;

namespace LogistiFlow.AiOperations.Application.Tools.Rules;

/// <summary>Fixed rules that turn a shipment's dates and exceptions into delay facts. No model is involved.</summary>
public static class ShipmentRiskRules
{
    public const string Delivered = "Delivered";

    public const string ReturnedToSender = "ReturnedToSender";

    public const string OpenStatus = "Open";

    public const string DelayType = "Delay";

    public static bool IsFinal(string status) => status is Delivered or ReturnedToSender;

    /// <summary>
    /// Days between the promised date and the delivery (when delivered) or the current estimate; positive means
    /// late. Null when either date is unknown.
    /// </summary>
    public static int? DaysLateVersusPromise(DateOnly? promised, DateOnly? estimated, DateTimeOffset? deliveredAt)
    {
        if (promised is not { } promise)
        {
            return null;
        }

        DateOnly? reference = deliveredAt is { } delivered ? DateOnly.FromDateTime(delivered.UtcDateTime) : estimated;
        return reference is { } date ? date.DayNumber - promise.DayNumber : null;
    }

    public static DelayAnalysisData Analyze(ShipmentView shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        var daysLate = DaysLateVersusPromise(shipment.PromisedDeliveryDate, shipment.EstimatedDelivery, shipment.DeliveredAt);
        var latestEvent = shipment.TrackingEvents.OrderBy(trackingEvent => trackingEvent.OccurredAt).LastOrDefault();

        return new DelayAnalysisData(
            shipment.OrderNumber,
            shipment.Status,
            shipment.CarrierName,
            shipment.TrackingNumber,
            shipment.PromisedDeliveryDate,
            shipment.OriginalEstimatedDelivery,
            shipment.EstimatedDelivery,
            shipment.DeliveredAt,
            daysLate,
            shipment.IsDelayed,
            [.. shipment.Exceptions.OrderBy(exception => exception.OpenedAt)],
            latestEvent,
            Assess(shipment.Status, shipment.IsDelayed, daysLate));
    }

    /// <summary>Why an undelivered shipment is at risk, or null when it is not.</summary>
    public static string? AtRiskReason(ShipmentListItemView shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        if (IsFinal(shipment.Status))
        {
            return null;
        }

        var daysLate = DaysLateVersusPromise(shipment.PromisedDeliveryDate, shipment.EstimatedDelivery, null);
        return (shipment.IsDelayed, daysLate) switch
        {
            (true, > 0) => string.Create(CultureInfo.InvariantCulture, $"Open carrier delay; estimate {daysLate} day(s) after the promised date"),
            (true, _) => "Open carrier delay",
            (false, > 0) => string.Create(CultureInfo.InvariantCulture, $"Estimate {daysLate} day(s) after the promised date"),
            _ => null,
        };
    }

    private static string Assess(string status, bool isDelayed, int? daysLate)
    {
        if (status == Delivered)
        {
            return daysLate > 0
                ? string.Create(CultureInfo.InvariantCulture, $"Delivered {daysLate} day(s) after the promised date.")
                : "Delivered on or before the promised date.";
        }

        if (status == ReturnedToSender)
        {
            return "Returned to sender; the order failed.";
        }

        if (isDelayed)
        {
            return daysLate > 0
                ? string.Create(CultureInfo.InvariantCulture, $"Delayed: the carrier reported a delay and the estimate is {daysLate} day(s) after the promised date.")
                : "Delayed: the carrier reported a delay, but the estimate still meets the promised date.";
        }

        return daysLate > 0
            ? string.Create(CultureInfo.InvariantCulture, $"At risk: the current estimate is {daysLate} day(s) after the promised date.")
            : "On schedule.";
    }
}
