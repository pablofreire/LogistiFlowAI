using System.ComponentModel;
using System.Diagnostics;
using LogistiFlow.McpServer.Contracts.Views;
using LogistiFlow.McpServer.Enums;
using LogistiFlow.McpServer.Interfaces;
using LogistiFlow.McpServer.Models;
using LogistiFlow.McpServer.Tools.Results;
using LogistiFlow.McpServer.Validators;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LogistiFlow.McpServer.Tools;

/// <summary>
/// The read-only LogistiFlow tools. Each one validates its arguments, reads through the gateway as the configured
/// user, and returns structured content with an output schema. A failure is a tool error whose message says why
/// (not found, not allowed, gateway unavailable), never invented data. Every call is logged with its outcome and
/// duration; arguments are identifiers only and no token is logged.
/// </summary>
[McpServerToolType]
public sealed partial class LogisticsTools(IGatewayClient gateway, ILogger<LogisticsTools> logger)
{
    [McpServerTool(Name = ToolNames.GetOrder, Title = "Get order", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets one LogistiFlow order by its number: customer, destination, promised date, current status and reason, lines, and the full status history (who changed it and why).")]
    public Task<OrderDetailsResult> GetOrderAsync(
        [Description("Order number, for example ORD-1025.")] string orderNumber,
        CancellationToken cancellationToken) =>
        RunAsync(ToolNames.GetOrder, orderNumber, async () =>
        {
            var order = Require(await gateway.FindOrderAsync(ToolArguments.OrderNumber(orderNumber), cancellationToken));
            var history = Require(await gateway.GetOrderHistoryAsync(order.Id, cancellationToken));
            return new OrderDetailsResult(order, history);
        });

    [McpServerTool(Name = ToolNames.GetInventory, Title = "Get inventory", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets stock for one SKU in every warehouse: on hand, reserved and available, plus the total available.")]
    public Task<SkuAvailabilityView> GetInventoryAsync(
        [Description("SKU, for example SKU-1003.")] string sku,
        CancellationToken cancellationToken) =>
        RunAsync(ToolNames.GetInventory, sku, async () =>
            Require(await gateway.GetAvailabilityAsync(ToolArguments.Sku(sku), cancellationToken)));

    [McpServerTool(Name = ToolNames.GetShipment, Title = "Get shipment", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets the shipment of an order: carrier, tracking number, status, promised, original and current estimated delivery dates, whether it is delayed, carrier tracking events and operational exceptions.")]
    public Task<ShipmentView> GetShipmentAsync(
        [Description("Order number, for example ORD-1025.")] string orderNumber,
        CancellationToken cancellationToken) =>
        RunAsync(ToolNames.GetShipment, orderNumber, async () =>
        {
            var order = Require(await gateway.FindOrderAsync(ToolArguments.OrderNumber(orderNumber), cancellationToken));
            var shipment = await gateway.GetShipmentByOrderAsync(order.Id, cancellationToken);
            return shipment.Outcome == GatewayOutcome.NotFound
                ? throw new McpException($"Order {order.OrderNumber} has no shipment yet; its status is {order.Status}.")
                : Require(shipment);
        });

    [McpServerTool(Name = ToolNames.ListExceptions, Title = "List exceptions", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists shipment exceptions (carrier unavailable, delay, failed delivery attempt, returned to sender), newest first, with the total that match.")]
    public Task<ExceptionListResult> ListExceptionsAsync(
        [Description("Optional status filter: Open or Resolved. Omit for both.")] string? status = null,
        [Description("Optional type filter: CarrierUnavailable, Delay, DeliveryAttemptFailed or ReturnedToSender. Omit for all.")] string? type = null,
        [Description("How many exceptions to return, 1 to 100. Default 20.")] int? limit = null,
        CancellationToken cancellationToken = default) =>
        RunAsync(ToolNames.ListExceptions, $"status={status}, type={type}, limit={limit}", async () =>
        {
            var statusFilter = ToolArguments.OneOf(status, ToolArguments.ExceptionStatuses, nameof(status));
            var typeFilter = ToolArguments.OneOf(type, ToolArguments.ExceptionTypes, nameof(type));
            var page = Require(await gateway.ListExceptionsAsync(statusFilter, typeFilter, ToolArguments.Limit(limit), cancellationToken));
            return new ExceptionListResult(statusFilter, typeFilter, page.TotalCount, page.Items);
        });

    [McpServerTool(Name = ToolNames.GetWarehouseMetrics, Title = "Get warehouse metrics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Gets fulfillment job counts per warehouse (open, failed, total) and per status, with the inventory total available and the number of low-stock positions.")]
    public Task<WarehouseMetricsResult> GetWarehouseMetricsAsync(CancellationToken cancellationToken) =>
        RunAsync(ToolNames.GetWarehouseMetrics, string.Empty, async () =>
        {
            var fulfillment = gateway.GetFulfillmentMetricsAsync(cancellationToken);
            var inventory = gateway.GetInventoryMetricsAsync(cancellationToken);
            var jobs = Require(await fulfillment);
            return new WarehouseMetricsResult(jobs.TotalJobs, jobs.ByStatus, jobs.ByWarehouse, Require(await inventory));
        });

    private static T Require<T>(GatewayReply<T> reply) => reply.IsOk ? reply.Data! : throw new McpException(reply.Message ?? "The gateway call failed.");

    private async Task<T> RunAsync<T>(string tool, string arguments, Func<Task<T>> body)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await body();
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogCompleted(logger, tool, arguments, elapsedMs);
            return result;
        }
        catch (McpException exception)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            LogRejected(logger, tool, arguments, exception.Message, elapsedMs);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tool {Tool} ({Arguments}) completed in {ElapsedMs:0} ms")]
    private static partial void LogCompleted(ILogger logger, string tool, string arguments, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tool {Tool} ({Arguments}) returned an error in {ElapsedMs:0} ms: {Reason}")]
    private static partial void LogRejected(ILogger logger, string tool, string arguments, string reason, double elapsedMs);
}
