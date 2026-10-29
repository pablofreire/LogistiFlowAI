using System.Net;
using LogistiFlow.Fulfillment.Api.Composition;
using LogistiFlow.IntegrationTests.Fixtures;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Messaging;

/// <summary>AC-28: a consuming service is not ready until its queues exist, so no event can be published past it.</summary>
public sealed class ConsumerReadinessTests(LogistiFlowEnvironment environment)
{
    [Fact]
    public async Task ConsumerThatCannotDeclareItsQueues_IsNotReady_ButStaysLive()
    {
        var settings = environment.Settings(Databases.Fulfillment, brokerPort: 1);
        settings["Messaging:Outbox:RelayEnabled"] = "false"; // keep this host away from the shared outbox
        await using var unreachableBroker = new ServiceFactory<FulfillmentApiMarker>(settings);
        using var client = unreachableBroker.CreateClient();

        (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ConsumerWithDeclaredQueues_IsReady()
    {
        using var client = environment.Shipping.CreateClient();

        (await client.GetAsync("/health/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
