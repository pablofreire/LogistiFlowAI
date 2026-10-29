using System.Net;
using System.Text.Json;
using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Connections;
using LogistiFlow.BuildingBlocks.Messaging.Connections;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Options;
using LogistiFlow.BuildingBlocks.Messaging.Outbox;
using LogistiFlow.BuildingBlocks.Messaging.Publishing;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using RabbitMQ.Client;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Messaging;

/// <summary>
/// The transactional outbox decouples accepting an order from the broker being up: the order is stored while
/// RabbitMQ is unreachable, the relay keeps the event and retries, and it is delivered once the broker is back.
/// </summary>
public sealed class OutboxResilienceTests(LogistiFlowEnvironment environment)
{
    private const string TestExchange = "logistiflow.tests.outbox";

    [Fact]
    public async Task OrderAcceptedWhileBrokerIsDown_IsPublishedWhenTheBrokerIsBack()
    {
        // The isolated Orders instance points at a closed port and has no relay of its own.
        using var client = environment.IsolatedOrders.CreateClientAs(LogistiFlowRoles.OpsManager);
        var response = await client.PostOrderAsync(ApiJson.OrderBody("SKU-1001", 1), $"outbox-{Guid.NewGuid():N}");
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = (await response.ReadDataAsync()).GetProperty("id").GetGuid().ToString();

        await using var db = await environment.OpenAsync(Databases.IsolatedOrders);
        const string Row = "SELECT attempts, published_at IS NOT NULL AS published, last_error AS lasterror FROM messaging.outbox_messages WHERE payload->>'orderId' = @orderId";
        (await db.QuerySingleAsync<OutboxRow>(Row, new { orderId })).Published.Should().BeFalse();

        await using var dataSource = NpgsqlDataSource.Create(environment.ConnectionString(Databases.IsolatedOrders));
        var connectionFactory = new NpgsqlConnectionFactory(dataSource);

        // Broker unreachable: nothing is lost, the failure is recorded and the row stays pending.
        await using (var down = Publisher(port: 1))
        {
            var relay = new OutboxRelay(connectionFactory, down.Publisher, RetryImmediately(), NullLogger<OutboxRelay>.Instance);
            (await relay.PublishPendingAsync(CancellationToken.None)).Should().Be(0);
        }

        var failed = await db.QuerySingleAsync<OutboxRow>(Row, new { orderId });
        failed.Published.Should().BeFalse();
        failed.Attempts.Should().BeGreaterThanOrEqualTo(1);
        failed.LastError.Should().NotBeNullOrEmpty();

        // Broker back: the same row is published, and a subscriber receives the event.
        await using var subscriber = await Subscriber.CreateAsync(environment, "orders.order-created.v1");
        await using (var up = Publisher(environment.RabbitPort))
        {
            var relay = new OutboxRelay(connectionFactory, up.Publisher, RetryImmediately(), NullLogger<OutboxRelay>.Instance);
            (await relay.PublishPendingAsync(CancellationToken.None)).Should().BeGreaterThanOrEqualTo(1);
        }

        (await db.QuerySingleAsync<OutboxRow>(Row, new { orderId })).Published.Should().BeTrue();
        var delivered = await subscriber.ReceiveAsync(envelope => envelope.Payload.GetProperty("orderId").GetString() == orderId);
        delivered.EventType.Should().Be("orders.order-created");
        delivered.Version.Should().Be(1);
    }

    private static IOptions<OutboxOptions> RetryImmediately() =>
        Options.Create(new OutboxOptions { BatchSize = 500, MaxRetryDelay = TimeSpan.Zero });

    private RelayPublisher Publisher(int port)
    {
        var rabbit = Options.Create(new RabbitMqOptions
        {
            HostName = environment.RabbitHost,
            Port = port,
            UserName = RabbitUser,
            Password = RabbitPassword,
            Exchange = TestExchange,
        });
        var connections = new RabbitMqConnectionProvider(rabbit, Options.Create(new MessagingOptions { ServiceName = "outbox-test" }));
        return new RelayPublisher(connections, new RabbitMqMessagePublisher(connections, rabbit));
    }

    private sealed record OutboxRow(int Attempts, bool Published, string? LastError);

    private sealed class RelayPublisher(RabbitMqConnectionProvider connections, RabbitMqMessagePublisher publisher) : IAsyncDisposable
    {
        public RabbitMqMessagePublisher Publisher => publisher;

        public async ValueTask DisposeAsync()
        {
            await publisher.DisposeAsync();
            await connections.DisposeAsync();
        }
    }

    private sealed class Subscriber(IConnection connection, IChannel channel, string queue) : IAsyncDisposable
    {
        public static async Task<Subscriber> CreateAsync(LogistiFlowEnvironment environment, string routingKey)
        {
            var factory = new ConnectionFactory { HostName = environment.RabbitHost, Port = environment.RabbitPort, UserName = RabbitUser, Password = RabbitPassword };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(TestExchange, ExchangeType.Topic, durable: true, autoDelete: false);
            var queue = (await channel.QueueDeclareAsync(string.Empty, durable: false, exclusive: true, autoDelete: true)).QueueName;
            await channel.QueueBindAsync(queue, TestExchange, routingKey);
            return new Subscriber(connection, channel, queue);
        }

        public async Task<MessageEnvelope> ReceiveAsync(Func<MessageEnvelope, bool> match)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline)
            {
                var message = await channel.BasicGetAsync(queue, autoAck: true);
                if (message is null)
                {
                    await Task.Delay(100);
                    continue;
                }

                var envelope = JsonSerializer.Deserialize<MessageEnvelope>(message.Body.Span, BuildingBlocks.Messaging.Serialization.MessagingJson.Options)!;
                if (match(envelope))
                {
                    return envelope;
                }
            }

            throw new TimeoutException("The expected message was not delivered.");
        }

        public async ValueTask DisposeAsync()
        {
            await channel.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
