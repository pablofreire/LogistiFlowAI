using LogistiFlow.Contracts.IntegrationEvents.Interfaces;

namespace LogistiFlow.Contracts.IntegrationEvents.Orders;

public sealed record OrderCancelledEvent(Guid OrderId, string OrderNumber, string Reason, DateTimeOffset CancelledAt) : IIntegrationEvent
{
    public static string EventType => "orders.order-cancelled";

    public static int Version => 1;
}
