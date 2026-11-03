namespace LogistiFlow.IntegrationTests.Fixtures;

/// <summary>
/// The gateway's proxy routes (<c>ProxyConfiguration</c>) without the gateway: each request goes, path unchanged, to
/// the in-memory server of the service that owns the path prefix. The services still validate the bearer token.
/// </summary>
public sealed class GatewayRoutingHandler(LogistiFlowEnvironment environment) : HttpMessageHandler
{
    private readonly HttpMessageInvoker _orders = new(environment.Orders.Server.CreateHandler());
    private readonly HttpMessageInvoker _inventory = new(environment.Inventory.Server.CreateHandler());
    private readonly HttpMessageInvoker _fulfillment = new(environment.Fulfillment.Server.CreateHandler());
    private readonly HttpMessageInvoker _shipping = new(environment.Shipping.Server.CreateHandler());

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = request.RequestUri!.AbsolutePath;
        var service = ServiceFor(path) ?? throw new InvalidOperationException($"The gateway has no route for {path}.");
        return service.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _orders.Dispose();
            _inventory.Dispose();
            _fulfillment.Dispose();
            _shipping.Dispose();
        }

        base.Dispose(disposing);
    }

    private HttpMessageInvoker? ServiceFor(string path) =>
        Owns(path, "orders") ? _orders
        : Owns(path, "inventory") ? _inventory
        : Owns(path, "fulfillment") ? _fulfillment
        : Owns(path, "shipping") ? _shipping
        : null;

    // Each service owns "/api/{service}/..." and "/api/metrics/{service}", as in ProxyConfiguration.
    private static bool Owns(string path, string service) =>
        path.StartsWith($"/api/{service}/", StringComparison.Ordinal)
        || path == $"/api/{service}"
        || path == $"/api/metrics/{service}";
}
