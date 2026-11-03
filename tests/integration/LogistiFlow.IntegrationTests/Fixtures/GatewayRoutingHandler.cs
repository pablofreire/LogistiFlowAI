namespace LogistiFlow.IntegrationTests.Fixtures;

/// <summary>
/// The gateway's proxy routes (<c>ProxyConfiguration</c>) without the gateway: each request goes, path unchanged, to
/// the in-memory server of the service that owns the path prefix. The services still validate the bearer token.
/// </summary>
public sealed class GatewayRoutingHandler(LogistiFlowEnvironment environment) : HttpMessageHandler
{
    private readonly (string Prefix, HttpMessageInvoker Service)[] _routes =
    [
        ("/api/orders", new HttpMessageInvoker(environment.Orders.Server.CreateHandler())),
        ("/api/metrics/orders", new HttpMessageInvoker(environment.Orders.Server.CreateHandler())),
        ("/api/inventory", new HttpMessageInvoker(environment.Inventory.Server.CreateHandler())),
        ("/api/metrics/inventory", new HttpMessageInvoker(environment.Inventory.Server.CreateHandler())),
        ("/api/fulfillment", new HttpMessageInvoker(environment.Fulfillment.Server.CreateHandler())),
        ("/api/metrics/fulfillment", new HttpMessageInvoker(environment.Fulfillment.Server.CreateHandler())),
        ("/api/shipping", new HttpMessageInvoker(environment.Shipping.Server.CreateHandler())),
        ("/api/metrics/shipping", new HttpMessageInvoker(environment.Shipping.Server.CreateHandler())),
    ];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = request.RequestUri!.AbsolutePath;
        var route = _routes.FirstOrDefault(route => path == route.Prefix || path.StartsWith(route.Prefix + "/", StringComparison.Ordinal));
        return route.Service is null
            ? throw new InvalidOperationException($"The gateway has no route for {path}.")
            : route.Service.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var (_, service) in _routes)
            {
                service.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
