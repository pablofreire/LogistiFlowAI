using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LogistiFlow.McpServer.Contracts.Gateway;
using LogistiFlow.McpServer.Contracts.Views;
using LogistiFlow.McpServer.Enums;
using LogistiFlow.McpServer.Exceptions;
using LogistiFlow.McpServer.Interfaces;
using LogistiFlow.McpServer.Models;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace LogistiFlow.McpServer.Gateway;

/// <summary>
/// GET calls through the gateway, with the bearer token added by <see cref="BearerTokenHandler"/>. Status codes
/// become <see cref="GatewayReply{T}"/> outcomes instead of exceptions; only the caller's own cancellation propagates.
/// </summary>
public sealed partial class GatewayClient(HttpClient httpClient, ILogger<GatewayClient> logger) : IGatewayClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<GatewayReply<OrderView>> FindOrderAsync(string orderNumber, CancellationToken cancellationToken)
    {
        var search = await GetAsync<GatewayPage<OrderSearchItemView>>($"api/orders?search={Uri.EscapeDataString(orderNumber)}&pageSize=20", cancellationToken);
        if (!search.IsOk)
        {
            return search.As<OrderView>();
        }

        var match = search.Data!.Items.FirstOrDefault(order => string.Equals(order.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? GatewayReply.Fail<OrderView>(GatewayOutcome.NotFound, $"Order {orderNumber} does not exist.")
            : await GetAsync<OrderView>($"api/orders/{match.Id}", cancellationToken);
    }

    public Task<GatewayReply<IReadOnlyList<StatusChangeView>>> GetOrderHistoryAsync(Guid orderId, CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<StatusChangeView>>($"api/orders/{orderId}/history", cancellationToken);

    public Task<GatewayReply<SkuAvailabilityView>> GetAvailabilityAsync(string sku, CancellationToken cancellationToken) =>
        GetAsync<SkuAvailabilityView>($"api/inventory/availability/{Uri.EscapeDataString(sku)}", cancellationToken);

    public Task<GatewayReply<ShipmentView>> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        GetAsync<ShipmentView>($"api/shipping/shipments/by-order/{orderId}", cancellationToken);

    public async Task<GatewayReply<ExceptionPage>> ListExceptionsAsync(string? status, string? type, int limit, CancellationToken cancellationToken)
    {
        var path = string.Create(CultureInfo.InvariantCulture, $"api/shipping/exceptions?pageSize={limit}")
                   + (status is null ? string.Empty : $"&status={Uri.EscapeDataString(status)}")
                   + (type is null ? string.Empty : $"&type={Uri.EscapeDataString(type)}");
        var page = await GetAsync<GatewayPage<ShipmentExceptionView>>(path, cancellationToken);
        return page.IsOk ? GatewayReply.Ok(new ExceptionPage(page.Data!.TotalCount, page.Data.Items)) : page.As<ExceptionPage>();
    }

    public Task<GatewayReply<FulfillmentMetricsView>> GetFulfillmentMetricsAsync(CancellationToken cancellationToken) =>
        GetAsync<FulfillmentMetricsView>("api/metrics/fulfillment", cancellationToken);

    public Task<GatewayReply<InventoryMetricsView>> GetInventoryMetricsAsync(CancellationToken cancellationToken) =>
        GetAsync<InventoryMetricsView>("api/metrics/inventory", cancellationToken);

    private async Task<GatewayReply<T>> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var envelope = await response.Content.ReadFromJsonAsync<GatewayEnvelope<T>>(Json, cancellationToken);
                return envelope is not null && envelope.Data is { } data
                    ? GatewayReply.Ok(data)
                    : GatewayReply.Fail<T>(GatewayOutcome.Unavailable, "The gateway returned an empty response.");
            }

            var error = await ReadErrorAsync(response, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => GatewayReply.Fail<T>(GatewayOutcome.NotFound, error ?? "Nothing was found."),
                HttpStatusCode.Forbidden => GatewayReply.Fail<T>(GatewayOutcome.Forbidden, "The signed-in LogistiFlow user is not allowed to read this."),
                HttpStatusCode.Unauthorized => GatewayReply.Fail<T>(GatewayOutcome.Unauthorized, "The gateway did not accept the server's access token."),
                HttpStatusCode.BadRequest => GatewayReply.Fail<T>(GatewayOutcome.Invalid, error ?? "The gateway rejected the request."),
                _ => Unavailable<T>(path, (int)response.StatusCode),
            };
        }
        catch (GatewaySignInException exception)
        {
            return GatewayReply.Fail<T>(GatewayOutcome.Unauthorized, exception.Message);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested
                                          && exception is HttpRequestException or TaskCanceledException or JsonException or BrokenCircuitException or TimeoutRejectedException)
        {
            LogFailed(logger, exception, path);
            return GatewayReply.Fail<T>(GatewayOutcome.Unavailable, "The LogistiFlow gateway is unavailable right now.");
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<GatewayEnvelope<JsonElement>>(Json, cancellationToken);
            return envelope?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private GatewayReply<T> Unavailable<T>(string path, int statusCode)
    {
        LogUnexpectedStatus(logger, path, statusCode);
        return GatewayReply.Fail<T>(GatewayOutcome.Unavailable, $"The LogistiFlow gateway is unavailable right now (HTTP {statusCode}).");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway call {Path} failed")]
    private static partial void LogFailed(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway call {Path} answered HTTP {StatusCode}")]
    private static partial void LogUnexpectedStatus(ILogger logger, string path, int statusCode);
}
