using System.Globalization;
using System.Text;
using LogistiFlow.AiOperations.Application.Agent;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Application.Tools;
using LogistiFlow.AiOperations.Application.Tools.Results;

namespace LogistiFlow.AiOperations.Infrastructure.Providers.Demo;

/// <summary>
/// Writes the demo provider's answers. Every fact comes from a tool result; when a tool did not return data the
/// answer says so and why. Each answer ends with a line that labels it as simulated.
/// </summary>
public static class DemoAnswerComposer
{
    public const string SimulatedFooter = "Simulated answer: deterministic demo provider (no AI model), built only from the tool results.";

    private const int MaxListed = 10;

    public static string Help()
    {
        var text = new StringBuilder("I am running in demo mode, without an AI model, and I recognise these questions:\n");
        foreach (var question in CopilotPrompts.SampleQuestions)
        {
            text.Append("- ").Append(question).Append('\n');
        }

        return text.Append('\n').Append(SimulatedFooter).ToString();
    }

    public static string Delay(ToolResult<DelayAnalysisData> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Data is not { } delay)
        {
            return Failed("analyse the shipment", result);
        }

        var text = new StringBuilder();
        Line(text, $"{delay.OrderNumber}: {delay.Assessment}");
        Line(text, $"Shipment {delay.ShipmentStatus} with {delay.CarrierName}, tracking {delay.TrackingNumber ?? "not assigned yet"}.");
        Line(text, $"Promised delivery: {Date(delay.PromisedDeliveryDate)}. Carrier estimate: {Date(delay.EstimatedDelivery)} (first estimate {Date(delay.OriginalEstimatedDelivery)}).");
        if (delay.DeliveredAt is { } delivered)
        {
            Line(text, $"Delivered at {Timestamp(delivered)}.");
        }

        if (delay.OpenExceptions.Count == 0)
        {
            Line(text, "Open exceptions: none.");
        }
        else
        {
            Line(text, "Open exceptions:");
            foreach (var exception in delay.OpenExceptions)
            {
                Line(text, $"- {exception.Type}: {exception.Details} (opened {Timestamp(exception.OpenedAt)})");
            }
        }

        if (delay.LatestEvent is { } latest)
        {
            Line(text, $"Latest tracking event: {latest.Description} at {latest.Location}, {Timestamp(latest.OccurredAt)}.");
        }

        return Finish(text);
    }

    public static string Reservation(ToolResult<OrderDetailsData> order, IReadOnlyList<ToolResult<SkuAvailabilityView>> availability)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(availability);
        if (order.Data is not { } details)
        {
            return Failed("read the order", order);
        }

        var text = new StringBuilder();
        var reservation = details.History.LastOrDefault(change => change.ToStatus is "Reserved" or "Failed" && change.FromStatus == "PendingInventory");
        if (details.Order.Status == "Failed")
        {
            Line(text, $"{details.Order.OrderNumber} failed: {details.Order.StatusReason ?? "no reason recorded"}.");
        }
        else
        {
            Line(text, $"{details.Order.OrderNumber} did not fail its reservation; it is {details.Order.Status}.");
        }

        if (reservation is not null)
        {
            Line(text, $"Reservation step: {reservation.FromStatus} -> {reservation.ToStatus} at {Timestamp(reservation.OccurredAt)}, from {reservation.Source}: {reservation.Reason ?? "no reason recorded"}.");
        }

        Line(text, "Ordered: " + string.Join(", ", details.Order.Lines.Select(line => Invariant($"{line.Sku} x {line.Quantity}"))) + ".");
        foreach (var stock in availability)
        {
            if (stock.Data is { } sku)
            {
                var warehouses = sku.Warehouses.Count == 0 ? "no warehouse stocks it" : string.Join(", ", sku.Warehouses.Select(warehouse => Invariant($"{warehouse.WarehouseCode} {warehouse.Available}")));
                Line(text, $"Stock of {sku.Sku} now: {sku.TotalAvailable} available ({warehouses}).");
            }
            else
            {
                Line(text, $"Stock could not be read: {stock.Message} ({stock.Status}).");
            }
        }

        return Finish(text);
    }

    public static string AtRisk(ToolResult<AtRiskShipmentsData> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Data is not { } data)
        {
            return Failed("list the shipments", result);
        }

        var text = new StringBuilder();
        if (data.Shipments.Count == 0)
        {
            Line(text, $"No shipment is at risk among the {data.Scanned} undelivered and recent shipments scanned.");
        }
        else
        {
            Line(text, $"{data.Shipments.Count} shipment(s) at risk of missing the promised date, out of {data.Scanned} scanned:");
            foreach (var shipment in data.Shipments.Take(MaxListed))
            {
                Line(text, $"- {shipment.OrderNumber} ({shipment.CarrierName}, {shipment.TrackingNumber ?? "no tracking yet"}, {shipment.Status}): {shipment.Reason}; promised {Date(shipment.PromisedDeliveryDate)}, estimate {Date(shipment.EstimatedDelivery)}.");
            }

            More(text, data.Shipments.Count);
        }

        if (data.Truncated)
        {
            Line(text, "Only the most recent shipments were scanned.");
        }

        return Finish(text);
    }

    public static string Warehouses(ToolResult<WarehouseMetricsData> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Data is not { } data)
        {
            return Failed("read the warehouse metrics", result);
        }

        var text = new StringBuilder();
        var worst = data.Warehouses.FirstOrDefault(warehouse => warehouse.WarehouseCode == data.MostFailures);
        Line(text, worst is null
            ? "No warehouse has failed fulfillment jobs."
            : $"{worst.WarehouseCode} has the most fulfillment failures: {worst.Failed} failed of {worst.Total} jobs.");
        foreach (var warehouse in data.Warehouses)
        {
            Line(text, $"- {warehouse.WarehouseCode}: {warehouse.Open} open, {warehouse.Failed} failed, {warehouse.Total} in total.");
        }

        Line(text, $"Low-stock positions: {data.LowStockPositions} (at or below {data.LowStockThreshold} units).");
        return Finish(text);
    }

    public static string Exceptions(ToolResult<ExceptionsSummaryData> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Data is not { } data)
        {
            return Failed("list the exceptions", result);
        }

        var text = new StringBuilder();
        if (data.Total == 0)
        {
            Line(text, $"No exceptions were opened in the last {data.SinceHours} hours.");
            return Finish(text);
        }

        Line(text, $"{data.Total} exception(s) opened in the last {data.SinceHours} hours.");
        Line(text, "By type: " + string.Join(", ", data.ByType.OrderByDescending(pair => pair.Value).Select(pair => Invariant($"{pair.Key} {pair.Value}"))) + ".");
        Line(text, "By status: " + string.Join(", ", data.ByStatus.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => Invariant($"{pair.Key} {pair.Value}"))) + ".");
        foreach (var exception in data.Items.Take(MaxListed))
        {
            Line(text, $"- {exception.OrderNumber} {exception.Type} ({exception.Status}, {Timestamp(exception.OpenedAt)}): {exception.Details}");
        }

        More(text, data.Items.Count);
        if (data.Truncated)
        {
            Line(text, "The window holds more exceptions than one page; only the most recent were read.");
        }

        return Finish(text);
    }

    public static string NextActions(ToolResult<RecoveryActionsData> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Data is not { } data)
        {
            return Failed("build suggestions", result);
        }

        var text = new StringBuilder();
        if (data.Actions.Count == 0)
        {
            Line(text, "No shipment needs action right now.");
            return Finish(text);
        }

        Line(text, "Suggested next steps (suggestions only; nothing has been changed):");
        foreach (var action in data.Actions.Take(MaxListed))
        {
            Line(text, $"- {action.OrderNumber}, {action.Problem}: {action.Action} Why: {action.Rationale} Where: {action.WhereInConsole}.");
        }

        More(text, data.Actions.Count);
        return Finish(text);
    }

    public static string Fulfillment(ToolResult<FulfillmentJobView> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Data is not { } job)
        {
            return Failed("read the warehouse job", result);
        }

        var text = new StringBuilder();
        Line(text, $"{job.OrderNumber}: warehouse job {job.Status} in {job.WarehouseCode}{(job.StatusReason is null ? string.Empty : $" ({job.StatusReason})")}.");
        if (job.History.Count > 0 && job.History[^1] is { } last)
        {
            Line(text, $"Last change: {last.FromStatus ?? "none"} -> {last.ToStatus} at {Timestamp(last.OccurredAt)}.");
        }

        return Finish(text);
    }

    public static string OrderStatus(ToolResult<OrderDetailsData> order, ToolResult<ShipmentView>? shipment)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Data is not { } details)
        {
            return Failed("read the order", order);
        }

        var text = new StringBuilder();
        Line(text, $"{details.Order.OrderNumber} for {details.Order.CustomerName} to {details.Order.DestinationCity}, {details.Order.DestinationCountry}: {details.Order.Status}{(details.Order.StatusReason is null ? string.Empty : $" ({details.Order.StatusReason})")}.");
        if (shipment?.Data is { } tracking)
        {
            Line(text, $"Shipment {tracking.Status} with {tracking.CarrierName}, tracking {tracking.TrackingNumber ?? "not assigned yet"}, estimate {Date(tracking.EstimatedDelivery)}.");
        }
        else if (shipment is not null)
        {
            Line(text, $"Shipment: {shipment.Message} ({shipment.Status}).");
        }

        return Finish(text);
    }

    private static string Failed<T>(string what, ToolResult<T> result) =>
        Finish(new StringBuilder(Invariant($"I could not {what}: {result.Message ?? "no data returned"} (status: {result.Status}). I will not guess the missing data.\n")));

    private static void More(StringBuilder text, int count)
    {
        if (count > MaxListed)
        {
            Line(text, $"...and {count - MaxListed} more.");
        }
    }

    private static string Finish(StringBuilder text) => text.Append('\n').Append(SimulatedFooter).ToString();

    private static void Line(StringBuilder text, FormattableString line) => text.Append(line.ToString(CultureInfo.InvariantCulture)).Append('\n');

    private static void Line(StringBuilder text, string line) => text.Append(line).Append('\n');

    private static string Invariant(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Date(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "unknown";

    private static string Timestamp(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
