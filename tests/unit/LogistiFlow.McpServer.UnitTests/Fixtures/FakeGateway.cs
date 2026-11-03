using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace LogistiFlow.McpServer.UnitTests.Fixtures;

/// <summary>
/// Stands in for the LogistiFlow gateway: canned envelopes per method and path, and a log of every request with its
/// query string, Authorization header and body. A path with no canned answer gets 404.
/// </summary>
public sealed class FakeGateway
{
    private readonly ConcurrentDictionary<string, Func<HttpResponseMessage>> _routes = new(StringComparer.Ordinal);

    public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

    public FakeGateway Ok(string path, object data, string method = "GET") =>
        On(method, path, HttpStatusCode.OK, new Envelope(data, null));

    public FakeGateway Fail(string path, HttpStatusCode status, string message, string method = "GET") =>
        On(method, path, status, new Envelope(null, new { code = "error", message }));

    public FakeGateway On(string method, string path, HttpStatusCode status, object body)
    {
        _routes[$"{method} {path}"] = () => new HttpResponseMessage(status) { Content = JsonContent.Create(body) };
        return this;
    }

    public FakeGateway Throws(string path, Exception exception)
    {
        _routes[$"GET {path}"] = () => throw exception;
        return this;
    }

    public IReadOnlyList<RecordedRequest> To(string path) => [.. Requests.Where(request => request.Path == path)];

    public HttpMessageHandler CreateHandler() => new Handler(this);

    private sealed class Handler(FakeGateway gateway) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var uri = request.RequestUri!;
            gateway.Requests.Enqueue(new RecordedRequest(request.Method.Method, uri.AbsolutePath, uri.Query, request.Headers.Authorization?.ToString(), body));

            return gateway._routes.TryGetValue($"{request.Method.Method} {uri.AbsolutePath}", out var respond)
                ? respond()
                : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = JsonContent.Create(new Envelope(null, new { code = "not_found", message = $"No fake route for {uri.AbsolutePath}." })) };
        }
    }

    /// <summary>The gateway's <c>{ data, error }</c> envelope.</summary>
    private sealed record Envelope(object? Data, object? Error);
}
