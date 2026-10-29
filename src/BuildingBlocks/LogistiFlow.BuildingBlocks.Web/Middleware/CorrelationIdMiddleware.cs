using LogistiFlow.BuildingBlocks.Application.Correlation.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.BuildingBlocks.Web.Middleware;

/// <summary>
/// Accepts an incoming <c>X-Correlation-Id</c> (or generates one), stores it in <see cref="ICorrelationContext"/>
/// so the outbox copies it into published events, writes it back to the request headers so a reverse proxy
/// forwards it downstream, echoes it in the response and adds it to the logging scope.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > MaxLength || !correlationId.All(IsSafe))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Request.Headers[HeaderName] = correlationId;
        context.RequestServices.GetService<ICorrelationContext>()?.Set(correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static bool IsSafe(char character) => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or ':' or '.';
}
