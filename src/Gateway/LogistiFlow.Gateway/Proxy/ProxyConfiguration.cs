using LogistiFlow.Gateway.Options;
using Yarp.ReverseProxy.Configuration;

namespace LogistiFlow.Gateway.Proxy;

/// <summary>
/// Routes built from <see cref="DownstreamServicesOptions"/>, the same addresses the dashboard uses, so there is a
/// single place to point the gateway at the services. The gateway rejects anonymous calls before proxying; each
/// service validates the forwarded token again (defence in depth).
/// </summary>
public static class ProxyConfiguration
{
    public const string AuthenticatedPolicy = "default";

    public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build(DownstreamServicesOptions services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var routes = new List<RouteConfig>
        {
            Route("orders", "orders", "/api/orders/{**rest}"),
            Route("orders-metrics", "orders", "/api/metrics/orders"),
            Route("inventory", "inventory", "/api/inventory/{**rest}"),
            Route("inventory-metrics", "inventory", "/api/metrics/inventory"),
            Route("fulfillment", "fulfillment", "/api/fulfillment/{**rest}"),
            Route("fulfillment-metrics", "fulfillment", "/api/metrics/fulfillment"),
            Route("shipping", "shipping", "/api/shipping/{**rest}"),
            Route("shipping-metrics", "shipping", "/api/metrics/shipping"),
            Route("aioperations", "aioperations", "/api/ai/{**rest}"),
        };

        var clusters = new List<ClusterConfig>
        {
            Cluster("orders", services.Orders),
            Cluster("inventory", services.Inventory),
            Cluster("fulfillment", services.Fulfillment),
            Cluster("shipping", services.Shipping),
            Cluster("aioperations", services.AiOperations),
        };

        return (routes, clusters);
    }

    private static RouteConfig Route(string id, string cluster, string path) => new()
    {
        RouteId = id,
        ClusterId = cluster,
        AuthorizationPolicy = AuthenticatedPolicy,
        Match = new RouteMatch { Path = path },
    };

    private static ClusterConfig Cluster(string id, Uri address) => new()
    {
        ClusterId = id,
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
        {
            ["primary"] = new DestinationConfig { Address = address.ToString() },
        },
        HttpRequest = new Yarp.ReverseProxy.Forwarder.ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(30) },
    };
}
