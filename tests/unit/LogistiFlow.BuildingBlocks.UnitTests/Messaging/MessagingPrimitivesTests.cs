using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Outbox;
using LogistiFlow.BuildingBlocks.Messaging.Topology;
using LogistiFlow.Contracts.IntegrationEvents.Orders;

namespace LogistiFlow.BuildingBlocks.UnitTests.Messaging;

public sealed class MessagingPrimitivesTests
{
    [Fact]
    public void RoutingKey_CombinesTypeAndVersion()
    {
        RoutingKeys.For(OrderCreatedEvent.EventType, OrderCreatedEvent.Version).Should().Be("orders.order-created.v1");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(6, 32)]
    [InlineData(7, 60)]
    [InlineData(50, 60)]
    public void OutboxBackoff_GrowsExponentiallyUpToTheCap(int attempts, int expectedSeconds)
    {
        OutboxRelay.BackoffFor(attempts, TimeSpan.FromSeconds(60)).Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void Topology_QueueNames_AreDerivedFromTheServiceQueue()
    {
        RabbitMqTopology.RetryQueue("orders.integration-events", 0).Should().Be("orders.integration-events.retry.1");
        RabbitMqTopology.DeadLetterQueue("orders.integration-events").Should().Be("orders.integration-events.dlq");
    }
}
