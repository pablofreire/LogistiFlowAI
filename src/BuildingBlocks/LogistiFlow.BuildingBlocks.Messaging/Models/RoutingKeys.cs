using System.Globalization;

namespace LogistiFlow.BuildingBlocks.Messaging.Models;

public static class RoutingKeys
{
    /// <summary><c>orders.order-created</c> version 1 is routed as <c>orders.order-created.v1</c>.</summary>
    public static string For(string eventType, int version) =>
        string.Create(CultureInfo.InvariantCulture, $"{eventType}.v{version}");
}
