using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Application.Tools;
using LogistiFlow.AiOperations.Application.Tools.Results;
using LogistiFlow.AiOperations.Infrastructure.Providers.Demo.Enums;
using LogistiFlow.AiOperations.Infrastructure.Providers.Demo.Models;

namespace LogistiFlow.AiOperations.Infrastructure.Providers.Demo;

/// <summary>
/// The demo provider's fixed rules: which tools a question needs, in which rounds, and when to answer. The answer
/// itself is written by <see cref="DemoAnswerComposer"/> from the tool results only.
/// </summary>
public static partial class DemoCopilotScript
{
    private const int DefaultHours = 24;
    private const int MaxSkus = 5;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// <paramref name="userMessages"/> holds the conversation's questions, oldest first; the last one is being
    /// answered. An order number missing from it is taken from earlier questions, so follow-ups work.
    /// <paramref name="results"/> holds this question's tool results by tool name, in the order they arrived.
    /// </summary>
    public static DemoStep Next(IReadOnlyList<string> userMessages, IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> results)
    {
        ArgumentNullException.ThrowIfNull(userMessages);
        ArgumentNullException.ThrowIfNull(results);

        var question = userMessages.Count == 0 ? string.Empty : userMessages[^1];
        var intent = Classify(question);
        var orderNumber = OrderNumberIn(question) ?? userMessages.Reverse().Skip(1).Select(OrderNumberIn).FirstOrDefault(number => number is not null);

        return intent switch
        {
            DemoIntent.ShipmentDelay => WithOrder(orderNumber, "Which order should I check? For example: Why is order ORD-1025 delayed?", number =>
                Single<DelayAnalysisData>(results, OperationsToolNames.AnalyzeShipmentDelay, Args(("orderNumber", number)), DemoAnswerComposer.Delay)),
            DemoIntent.ReservationFailure => WithOrder(orderNumber, "Which order failed? For example: What caused the inventory reservation failure for ORD-1025?", number =>
                ReservationFailure(results, number)),
            DemoIntent.ShipmentsAtRisk => Single<AtRiskShipmentsData>(results, OperationsToolNames.ListShipmentsAtRisk, Args(), DemoAnswerComposer.AtRisk),
            DemoIntent.WarehouseFailures => Single<WarehouseMetricsData>(results, OperationsToolNames.GetWarehouseMetrics, Args(), DemoAnswerComposer.Warehouses),
            DemoIntent.ExceptionsSummary => Single<ExceptionsSummaryData>(results, OperationsToolNames.ListOperationalExceptions, Args(("sinceHours", HoursIn(question))), DemoAnswerComposer.Exceptions),
            DemoIntent.NextActions => Single<RecoveryActionsData>(results, OperationsToolNames.SuggestRecoveryActions, Args(("orderNumber", orderNumber)), DemoAnswerComposer.NextActions),
            DemoIntent.FulfillmentStatus => WithOrder(orderNumber, "Which order should I check? For example: What is the fulfillment status of ORD-1025?", number =>
                Single<FulfillmentJobView>(results, OperationsToolNames.GetFulfillmentStatus, Args(("orderNumber", number)), DemoAnswerComposer.Fulfillment)),
            DemoIntent.OrderStatus => WithOrder(orderNumber, DemoAnswerComposer.Help(), number => OrderStatus(results, number)),
            _ => DemoStep.Reply(DemoAnswerComposer.Help()),
        };
    }

    public static DemoIntent Classify(string question)
    {
        var text = (question ?? string.Empty).ToLowerInvariant();
        var hasOrder = OrderNumberIn(text) is not null;

        if (ContainsAny(text, "suggest", "next action", "next step", "recover", "what should"))
        {
            return DemoIntent.NextActions;
        }

        if (ContainsAny(text, "reservation", "out of stock", "shortage", "inventory"))
        {
            return DemoIntent.ReservationFailure;
        }

        if (ContainsAny(text, "at risk", "deadline", "miss"))
        {
            return DemoIntent.ShipmentsAtRisk;
        }

        if (text.Contains("warehouse", StringComparison.Ordinal) && ContainsAny(text, "fail", "highest", "most", "worst"))
        {
            return DemoIntent.WarehouseFailures;
        }

        if (text.Contains("exception", StringComparison.Ordinal))
        {
            return DemoIntent.ExceptionsSummary;
        }

        if (ContainsAny(text, "delay", "late", "why"))
        {
            return DemoIntent.ShipmentDelay;
        }

        if (ContainsAny(text, "fulfillment", "picking", "packed", "warehouse job"))
        {
            return DemoIntent.FulfillmentStatus;
        }

        return hasOrder ? DemoIntent.OrderStatus : DemoIntent.Unknown;
    }

    public static string? OrderNumberIn(string? text)
    {
        var match = OrderNumberPattern().Match(text ?? string.Empty);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    public static int HoursIn(string question)
    {
        var text = (question ?? string.Empty).ToLowerInvariant();
        var match = HoursPattern().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hours))
        {
            return Math.Clamp(hours, 1, 168);
        }

        return text.Contains("week", StringComparison.Ordinal) ? 168 : DefaultHours;
    }

    private static DemoStep WithOrder(string? orderNumber, string ask, Func<string, DemoStep> plan) =>
        orderNumber is null ? DemoStep.Reply(ask) : plan(orderNumber);

    private static DemoStep Single<T>(
        IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> results,
        string tool,
        IReadOnlyDictionary<string, object?> arguments,
        Func<ToolResult<T>, string> compose) =>
        results.TryGetValue(tool, out var done) && done.Count > 0
            ? DemoStep.Reply(compose(Read<T>(done[0])))
            : DemoStep.Run(new DemoToolCall(tool, arguments));

    private static DemoStep ReservationFailure(IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> results, string orderNumber)
    {
        if (!results.TryGetValue(OperationsToolNames.GetOrderDetails, out var orderResults) || orderResults.Count == 0)
        {
            return DemoStep.Run(new DemoToolCall(OperationsToolNames.GetOrderDetails, Args(("orderNumber", orderNumber))));
        }

        var order = Read<OrderDetailsData>(orderResults[0]);
        var skus = order.Data?.Order.Lines.Select(line => line.Sku).Distinct(StringComparer.Ordinal).Take(MaxSkus).ToList() ?? [];
        var availability = results.GetValueOrDefault(OperationsToolNames.GetInventoryAvailability) ?? [];

        // Second round: once the order's lines are known, read the stock of each of its SKUs.
        if (order.Status == ToolResult.OkStatus && skus.Count > 0 && availability.Count == 0)
        {
            return DemoStep.Run([.. skus.Select(sku => new DemoToolCall(OperationsToolNames.GetInventoryAvailability, Args(("sku", sku))))]);
        }

        return DemoStep.Reply(DemoAnswerComposer.Reservation(order, [.. availability.Select(Read<SkuAvailabilityView>)]));
    }

    private static DemoStep OrderStatus(IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> results, string orderNumber)
    {
        if (!results.ContainsKey(OperationsToolNames.GetOrderDetails))
        {
            return DemoStep.Run(
                new DemoToolCall(OperationsToolNames.GetOrderDetails, Args(("orderNumber", orderNumber))),
                new DemoToolCall(OperationsToolNames.GetShipmentTracking, Args(("orderNumber", orderNumber))));
        }

        var order = Read<OrderDetailsData>(results[OperationsToolNames.GetOrderDetails][0]);
        var shipment = results.TryGetValue(OperationsToolNames.GetShipmentTracking, out var tracking) && tracking.Count > 0
            ? Read<ShipmentView>(tracking[0])
            : null;
        return DemoStep.Reply(DemoAnswerComposer.OrderStatus(order, shipment));
    }

    private static ToolResult<T> Read<T>(JsonElement element) =>
        element.Deserialize<ToolResult<T>>(Json) ?? new ToolResult<T>("unavailable", "The tool returned nothing.", default);

    private static Dictionary<string, object?> Args(params (string Name, object? Value)[] arguments) =>
        arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal);

    private static bool ContainsAny(string text, params string[] words) => words.Any(word => text.Contains(word, StringComparison.Ordinal));

    [GeneratedRegex(@"\bORD-[0-9]{1,10}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderNumberPattern();

    [GeneratedRegex(@"(\d{1,3})\s*(?:h\b|hours?)", RegexOptions.CultureInvariant)]
    private static partial Regex HoursPattern();
}
