using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Application.Options;
using LogistiFlow.AiOperations.Application.Tools.Results;
using LogistiFlow.AiOperations.Application.Tools.Rules;
using LogistiFlow.AiOperations.Domain.Enums;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LogistiFlow.AiOperations.Application.Tools;

/// <summary>
/// The copilot's read-only tools. Each validates its arguments, reads the services as the calling user, returns
/// a typed <see cref="ToolResult{T}"/> and records the call (arguments, outcome, duration) in
/// <see cref="CopilotToolCallLog"/>. Nothing here changes state.
/// </summary>
public sealed partial class OperationsToolset(
    IOrdersApi orders,
    IInventoryApi inventory,
    IFulfillmentApi fulfillment,
    IShippingApi shipping,
    CopilotToolCallLog log,
    IOptions<CopilotOptions> options,
    TimeProvider timeProvider)
{
    private const int MaxSinceHours = 168;
    private const int MaxShipmentsForSuggestions = 5;

    private static readonly JsonSerializerOptions ArgumentJson = new(JsonSerializerDefaults.Web);

    /// <summary>The tools as the agent framework sees them. Calls are recorded in the question's <see cref="CopilotToolCallLog"/>.</summary>
    public IList<AITool> CreateTools() =>
        [
            AIFunctionFactory.Create(GetOrderDetailsAsync, OperationsToolNames.GetOrderDetails, "Gets an order by number: status, status reason, destination, lines and its status timeline."),
            AIFunctionFactory.Create(GetInventoryAvailabilityAsync, OperationsToolNames.GetInventoryAvailability, "Gets the available stock of a SKU in every warehouse."),
            AIFunctionFactory.Create(GetFulfillmentStatusAsync, OperationsToolNames.GetFulfillmentStatus, "Gets the warehouse job of an order: warehouse, status, reason and history."),
            AIFunctionFactory.Create(GetShipmentTrackingAsync, OperationsToolNames.GetShipmentTracking, "Gets the shipment of an order: carrier, tracking number, tracking events, estimates and exceptions."),
            AIFunctionFactory.Create(AnalyzeShipmentDelayAsync, OperationsToolNames.AnalyzeShipmentDelay, "Explains whether and why an order's shipment is late: promised and estimated dates, days late and open delay exceptions."),
            AIFunctionFactory.Create(ListShipmentsAtRiskAsync, OperationsToolNames.ListShipmentsAtRisk, "Lists undelivered shipments with an open carrier delay or an estimate after the promised date."),
            AIFunctionFactory.Create(ListOperationalExceptionsAsync, OperationsToolNames.ListOperationalExceptions, "Lists shipment exceptions opened in the last N hours, with counts by type and status."),
            AIFunctionFactory.Create(GetWarehouseMetricsAsync, OperationsToolNames.GetWarehouseMetrics, "Gets open and failed fulfillment jobs per warehouse and the number of low-stock positions."),
            AIFunctionFactory.Create(SuggestRecoveryActionsAsync, OperationsToolNames.SuggestRecoveryActions, "Suggests next steps from a fixed playbook for one order, or for the shipments at risk when no order is given. Suggests only; executes nothing."),
        ];

    public Task<ToolResult<OrderDetailsData>> GetOrderDetailsAsync(
        [Description("Order number, for example ORD-1025")] string orderNumber,
        CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.GetOrderDetails, new { orderNumber }, async () =>
        {
            if (Normalize(orderNumber) is not { } number)
            {
                return InvalidOrderNumber<OrderDetailsData>(orderNumber);
            }

            var order = await orders.FindByNumberAsync(number, cancellationToken);
            if (!order.IsOk)
            {
                return ToolResult.From<OrderDetailsData>(order);
            }

            var history = await orders.GetHistoryAsync(order.Data!.Id, cancellationToken);
            return history.IsOk
                ? ToolResult.Ok(new OrderDetailsData(order.Data, history.Data!))
                : ToolResult.From<OrderDetailsData>(history);
        }, data => $"{data.Order.OrderNumber}: {data.Order.Status}");

    public Task<ToolResult<SkuAvailabilityView>> GetInventoryAvailabilityAsync(
        [Description("Product SKU, for example SKU-1001")] string sku,
        CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.GetInventoryAvailability, new { sku }, async () =>
        {
            if (string.IsNullOrWhiteSpace(sku) || !SkuPattern().IsMatch(sku.Trim()))
            {
                return ToolResult.Fail<SkuAvailabilityView>(CopilotToolCallOutcome.Invalid, "A SKU has letters, digits and dashes, up to 40 characters.");
            }

            var availability = await inventory.GetAvailabilityAsync(sku.Trim().ToUpperInvariant(), cancellationToken);
            return availability.IsOk ? ToolResult.Ok(availability.Data!) : ToolResult.From<SkuAvailabilityView>(availability);
        }, data => string.Create(CultureInfo.InvariantCulture, $"{data.Sku}: {data.TotalAvailable} available in {data.Warehouses.Count} warehouse(s)"));

    public Task<ToolResult<FulfillmentJobView>> GetFulfillmentStatusAsync(
        [Description("Order number, for example ORD-1025")] string orderNumber,
        CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.GetFulfillmentStatus, new { orderNumber }, async () =>
        {
            var order = await ResolveOrderAsync(orderNumber, cancellationToken);
            if (!order.IsOk)
            {
                return ToolResult.From<FulfillmentJobView>(order);
            }

            var job = await fulfillment.GetJobByOrderAsync(order.Data!.Id, cancellationToken);
            return job.IsOk
                ? ToolResult.Ok(job.Data!)
                : ToolResult.From<FulfillmentJobView>(job.Outcome == CopilotToolCallOutcome.NotFound
                    ? ServiceReply.Fail<FulfillmentJobView>(CopilotToolCallOutcome.NotFound, $"{order.Data.OrderNumber} has no warehouse job; it is {order.Data.Status}.")
                    : job);
        }, data => $"{data.OrderNumber}: job {data.Status} in {data.WarehouseCode}");

    public Task<ToolResult<ShipmentView>> GetShipmentTrackingAsync(
        [Description("Order number, for example ORD-1025")] string orderNumber,
        CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.GetShipmentTracking, new { orderNumber }, async () =>
        {
            var shipment = await ShipmentOfAsync(orderNumber, cancellationToken);
            return shipment.IsOk ? ToolResult.Ok(shipment.Data!) : ToolResult.From<ShipmentView>(shipment);
        }, data => $"{data.OrderNumber}: shipment {data.Status} with {data.CarrierCode}");

    public Task<ToolResult<DelayAnalysisData>> AnalyzeShipmentDelayAsync(
        [Description("Order number, for example ORD-1025")] string orderNumber,
        CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.AnalyzeShipmentDelay, new { orderNumber }, async () =>
        {
            var shipment = await ShipmentOfAsync(orderNumber, cancellationToken);
            return shipment.IsOk ? ToolResult.Ok(ShipmentRiskRules.Analyze(shipment.Data!)) : ToolResult.From<DelayAnalysisData>(shipment);
        }, data => $"{data.OrderNumber}: {data.Assessment}");

    public Task<ToolResult<AtRiskShipmentsData>> ListShipmentsAtRiskAsync(CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.ListShipmentsAtRisk, new { }, async () =>
        {
            var atRisk = await FindAtRiskAsync(cancellationToken);
            return atRisk.IsOk ? ToolResult.Ok(atRisk.Data!) : ToolResult.From<AtRiskShipmentsData>(atRisk);
        }, data => string.Create(CultureInfo.InvariantCulture, $"{data.Shipments.Count} at risk of {data.Scanned} scanned"));

    public Task<ToolResult<ExceptionsSummaryData>> ListOperationalExceptionsAsync(
        [Description("Window in hours, 1 to 168. Use 24 for the last day.")] int sinceHours,
        CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.ListOperationalExceptions, new { sinceHours }, async () =>
        {
            if (sinceHours is < 1 or > MaxSinceHours)
            {
                return ToolResult.Fail<ExceptionsSummaryData>(CopilotToolCallOutcome.Invalid, "sinceHours must be between 1 and 168.");
            }

            var page = await shipping.ListExceptionsAsync(options.Value.ScanLimit, cancellationToken);
            if (!page.IsOk)
            {
                return ToolResult.From<ExceptionsSummaryData>(page);
            }

            var since = timeProvider.GetUtcNow().AddHours(-sinceHours);
            var items = page.Data!.Items.Where(exception => exception.OpenedAt >= since).ToList();

            // The page is newest first: it may have cut the window only if its oldest item is still inside it.
            var truncated = page.Data.IsTruncated && items.Count == page.Data.Items.Count;
            return ToolResult.Ok(new ExceptionsSummaryData(
                sinceHours,
                items.Count,
                CountBy(items, exception => exception.Type),
                CountBy(items, exception => exception.Status),
                items,
                truncated));
        }, data => string.Create(CultureInfo.InvariantCulture, $"{data.Total} exception(s) in {data.SinceHours} h"));

    public Task<ToolResult<WarehouseMetricsData>> GetWarehouseMetricsAsync(CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.GetWarehouseMetrics, new { }, async () =>
        {
            var jobs = await fulfillment.GetMetricsAsync(cancellationToken);
            if (!jobs.IsOk)
            {
                return ToolResult.From<WarehouseMetricsData>(jobs);
            }

            var stock = await inventory.GetMetricsAsync(cancellationToken);
            if (!stock.IsOk)
            {
                return ToolResult.From<WarehouseMetricsData>(stock);
            }

            var warehouses = jobs.Data!.ByWarehouse.OrderByDescending(warehouse => warehouse.Failed).ThenBy(warehouse => warehouse.WarehouseCode, StringComparer.Ordinal).ToList();
            var mostFailures = warehouses.FirstOrDefault(warehouse => warehouse.Failed > 0)?.WarehouseCode;
            return ToolResult.Ok(new WarehouseMetricsData(warehouses, mostFailures, stock.Data!.LowStockPositions, stock.Data.LowStockThreshold));
        }, data => data.MostFailures is null ? "no failed jobs" : $"most failures: {data.MostFailures}");

    public Task<ToolResult<RecoveryActionsData>> SuggestRecoveryActionsAsync(
        [Description("Order number, for example ORD-1025. Omit to cover the shipments at risk.")] string? orderNumber,
        CancellationToken cancellationToken) =>
        RunAsync(OperationsToolNames.SuggestRecoveryActions, new { orderNumber }, async () =>
        {
            if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                var order = await ResolveOrderAsync(orderNumber, cancellationToken);
                if (!order.IsOk)
                {
                    return ToolResult.From<RecoveryActionsData>(order);
                }

                var shipment = await shipping.GetShipmentByOrderAsync(order.Data!.Id, cancellationToken);
                return shipment.Outcome switch
                {
                    CopilotToolCallOutcome.Ok => ToolResult.Ok(new RecoveryActionsData(RecoveryPlaybook.ForShipment(shipment.Data!), 1)),
                    CopilotToolCallOutcome.NotFound => ToolResult.Ok(new RecoveryActionsData(RecoveryPlaybook.ForOrderWithoutShipment(order.Data), 0)),
                    _ => ToolResult.From<RecoveryActionsData>(shipment),
                };
            }

            var atRisk = await FindAtRiskAsync(cancellationToken);
            if (!atRisk.IsOk)
            {
                return ToolResult.From<RecoveryActionsData>(atRisk);
            }

            var actions = new List<RecoveryAction>();
            foreach (var item in atRisk.Data!.Shipments.Take(MaxShipmentsForSuggestions))
            {
                var shipment = await shipping.GetShipmentAsync(item.ShipmentId, cancellationToken);
                if (shipment.IsOk)
                {
                    actions.AddRange(RecoveryPlaybook.ForShipment(shipment.Data!));
                }
            }

            return ToolResult.Ok(new RecoveryActionsData(actions, Math.Min(atRisk.Data.Shipments.Count, MaxShipmentsForSuggestions)));
        }, data => string.Create(CultureInfo.InvariantCulture, $"{data.Actions.Count} suggestion(s) for {data.ShipmentsConsidered} shipment(s)"));

    private static Dictionary<string, int> CountBy<T>(IEnumerable<T> items, Func<T, string> key) =>
        items.GroupBy(key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    private static string? Normalize(string? orderNumber)
    {
        var trimmed = orderNumber?.Trim().ToUpperInvariant();
        return trimmed is not null && OrderNumberPattern().IsMatch(trimmed) ? trimmed : null;
    }

    private static ToolResult<T> InvalidOrderNumber<T>(string? orderNumber) =>
        ToolResult.Fail<T>(CopilotToolCallOutcome.Invalid, $"'{orderNumber}' is not an order number; they look like ORD-1025.");

    [GeneratedRegex("^ORD-[0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex OrderNumberPattern();

    [GeneratedRegex("^[A-Za-z0-9-]{1,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex SkuPattern();

    private async Task<ServiceReply<OrderView>> ResolveOrderAsync(string? orderNumber, CancellationToken cancellationToken) =>
        Normalize(orderNumber) is { } number
            ? await orders.FindByNumberAsync(number, cancellationToken)
            : ServiceReply.Fail<OrderView>(CopilotToolCallOutcome.Invalid, $"'{orderNumber}' is not an order number; they look like ORD-1025.");

    private async Task<ServiceReply<ShipmentView>> ShipmentOfAsync(string? orderNumber, CancellationToken cancellationToken)
    {
        var order = await ResolveOrderAsync(orderNumber, cancellationToken);
        if (!order.IsOk)
        {
            return order.As<ShipmentView>();
        }

        var shipment = await shipping.GetShipmentByOrderAsync(order.Data!.Id, cancellationToken);
        return shipment.Outcome == CopilotToolCallOutcome.NotFound
            ? ServiceReply.Fail<ShipmentView>(CopilotToolCallOutcome.NotFound, $"{order.Data.OrderNumber} has no shipment; it is {order.Data.Status}.")
            : shipment;
    }

    private async Task<ServiceReply<AtRiskShipmentsData>> FindAtRiskAsync(CancellationToken cancellationToken)
    {
        // Two newest-first pages: every shipment with an open delay, then recent shipments whose estimate slipped.
        var delayed = await shipping.ListShipmentsAsync(delayedOnly: true, options.Value.ScanLimit, cancellationToken);
        if (!delayed.IsOk)
        {
            return delayed.As<AtRiskShipmentsData>();
        }

        var recent = await shipping.ListShipmentsAsync(delayedOnly: false, options.Value.ScanLimit, cancellationToken);
        if (!recent.IsOk)
        {
            return recent.As<AtRiskShipmentsData>();
        }

        var scanned = delayed.Data!.Items.Concat(recent.Data!.Items).DistinctBy(shipment => shipment.Id).ToList();
        var atRisk = scanned
            .Select(shipment => (Shipment: shipment, Reason: ShipmentRiskRules.AtRiskReason(shipment)))
            .Where(candidate => candidate.Reason is not null)
            .Select(candidate => new AtRiskShipment(
                candidate.Shipment.Id,
                candidate.Shipment.OrderNumber,
                candidate.Shipment.CarrierName,
                candidate.Shipment.TrackingNumber,
                candidate.Shipment.Status,
                candidate.Shipment.PromisedDeliveryDate,
                candidate.Shipment.EstimatedDelivery,
                ShipmentRiskRules.DaysLateVersusPromise(candidate.Shipment.PromisedDeliveryDate, candidate.Shipment.EstimatedDelivery, null),
                candidate.Shipment.IsDelayed,
                candidate.Shipment.OpenExceptions,
                candidate.Reason!))
            .OrderByDescending(shipment => shipment.DaysLateVersusPromise ?? 0)
            .ThenBy(shipment => shipment.OrderNumber, StringComparer.Ordinal)
            .ToList();

        return ServiceReply.Ok(new AtRiskShipmentsData(atRisk, scanned.Count, delayed.Data.IsTruncated || recent.Data.IsTruncated));
    }

    private async Task<ToolResult<T>> RunAsync<T>(string toolName, object arguments, Func<Task<ToolResult<T>>> body, Func<T, string> summarize)
    {
        var started = Stopwatch.GetTimestamp();
        ToolResult<T> result;
        try
        {
            result = await body();
        }
        catch (OperationCanceledException)
        {
            log.Record(toolName, JsonSerializer.Serialize(arguments, ArgumentJson), CopilotToolCallOutcome.Unavailable, "cancelled or timed out", ElapsedMs(started), timeProvider.GetUtcNow());
            throw;
        }

        var outcome = ToolResult.OutcomeOf(result.Status);
        var summary = result.Data is { } data ? summarize(data) : result.Message ?? result.Status;
        log.Record(toolName, JsonSerializer.Serialize(arguments, ArgumentJson), outcome, summary, ElapsedMs(started), timeProvider.GetUtcNow());
        return result;
    }

    private static long ElapsedMs(long started) => (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
