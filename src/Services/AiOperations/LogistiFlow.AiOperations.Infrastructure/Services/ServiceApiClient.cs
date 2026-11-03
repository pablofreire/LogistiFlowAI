using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Domain.Enums;
using LogistiFlow.AiOperations.Infrastructure.Services.Contracts;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace LogistiFlow.AiOperations.Infrastructure.Services;

/// <summary>
/// GET calls to one service, with the caller's token added by the client pipeline. Status codes become
/// <see cref="ServiceReply{T}"/> outcomes instead of exceptions, so a tool can tell the model exactly why it has no
/// data. Only the caller's own cancellation propagates.
/// </summary>
public abstract partial class ServiceApiClient(HttpClient httpClient, ILogger logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected abstract string ServiceName { get; }

    protected async Task<ServiceReply<T>> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var envelope = await response.Content.ReadFromJsonAsync<ServiceEnvelope<T>>(Json, cancellationToken);
                return envelope is not null && envelope.Data is { } data
                    ? ServiceReply.Ok(data)
                    : ServiceReply.Fail<T>(CopilotToolCallOutcome.Unavailable, $"{ServiceName} returned an empty response.");
            }

            var error = await ReadErrorAsync(response, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ServiceReply.Fail<T>(CopilotToolCallOutcome.NotFound, error ?? $"{ServiceName} found nothing."),
                HttpStatusCode.Forbidden => ServiceReply.Fail<T>(CopilotToolCallOutcome.Forbidden, $"{ServiceName} does not allow this user to read it."),
                HttpStatusCode.Unauthorized => ServiceReply.Fail<T>(CopilotToolCallOutcome.Forbidden, $"{ServiceName} did not accept the user's sign-in."),
                HttpStatusCode.BadRequest => ServiceReply.Fail<T>(CopilotToolCallOutcome.Invalid, error ?? $"{ServiceName} rejected the request."),
                _ => Unavailable<T>((int)response.StatusCode),
            };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested
                                          && exception is HttpRequestException or TaskCanceledException or JsonException or BrokenCircuitException or TimeoutRejectedException)
        {
            LogFailed(logger, exception, ServiceName, path);
            return ServiceReply.Fail<T>(CopilotToolCallOutcome.Unavailable, $"{ServiceName} is unavailable right now.");
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<ServiceEnvelope<JsonElement>>(Json, cancellationToken);
            return envelope?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private ServiceReply<T> Unavailable<T>(int statusCode)
    {
        LogUnexpectedStatus(logger, ServiceName, statusCode);
        return ServiceReply.Fail<T>(CopilotToolCallOutcome.Unavailable, $"{ServiceName} is unavailable right now (HTTP {statusCode}).");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Service} call {Path} failed")]
    private static partial void LogFailed(ILogger logger, Exception exception, string service, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Service} answered HTTP {StatusCode}")]
    private static partial void LogUnexpectedStatus(ILogger logger, string service, int statusCode);
}
