using System.Text;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using LogistiFlow.BuildingBlocks.Messaging.DependencyInjection;
using LogistiFlow.BuildingBlocks.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Serialization;
using LogistiFlow.IntegrationTests.Fixtures;
using LogistiFlow.IntegrationTests.Messaging.Probes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using static LogistiFlow.IntegrationTests.Fixtures.LogistiFlowEnvironment;

namespace LogistiFlow.IntegrationTests.Messaging;

/// <summary>
/// A handler that keeps failing is retried through the backoff tiers and then dead-lettered; a message that
/// cannot be read goes straight to the dead-letter queue. Nothing is lost and nothing loops forever.
/// </summary>
public sealed class RetryAndDeadLetterTests(LogistiFlowEnvironment environment) : IAsyncLifetime
{
    private const string Queue = "probe.integration-events";
    private const int MaxAttempts = 3;

    private IHost _host = null!;
    private IConnection _connection = null!;
    private IChannel _channel = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        var settings = environment.Settings(Databases.Probe);
        settings["Messaging:Consumer:MaxDeliveryAttempts"] = MaxAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture);
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddSingleton<ProbeAttempts>();
        builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);
        builder.Services.AddLogistiFlowMessaging(builder.Configuration, "probe", handlers => handlers.Add<ProbeEvent, FailingProbeEventHandler>());
        _host = builder.Build();
        await _host.StartAsync();

        var factory = new ConnectionFactory { HostName = environment.RabbitHost, Port = environment.RabbitPort, UserName = RabbitUser, Password = RabbitPassword };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        await EventuallyAsync(QueueExistsAsync, exists => exists, "the probe queue to be declared");
        await _channel.QueuePurgeAsync($"{Queue}.dlq");
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact]
    public async Task FailingHandler_IsRetriedWithBackoff_ThenDeadLettered()
    {
        var publisher = _host.Services.GetRequiredService<IMessagePublisher>();
        var envelope = new MessageEnvelope(
            Guid.CreateVersion7(),
            ProbeEvent.EventType,
            ProbeEvent.Version,
            DateTimeOffset.UtcNow,
            "dead-letter-test",
            null,
            JsonSerializer.SerializeToElement(new ProbeEvent("fail me"), MessagingJson.Options));

        await publisher.PublishAsync(envelope, null, CancellationToken.None);

        var deadLetter = await EventuallyAsync(
            () => _channel.BasicGetAsync($"{Queue}.dlq", autoAck: true),
            message => message is not null,
            "the message to reach the dead-letter queue");

        _host.Services.GetRequiredService<ProbeAttempts>().Seen.Should().Equal(1, 2, 3);
        deadLetter!.BasicProperties.Headers![MessageHeaders.Attempt].Should().Be(MaxAttempts);
        Encoding.UTF8.GetString((byte[])deadLetter.BasicProperties.Headers[MessageHeaders.DeadLetterReason]!).Should().Be("probe failure");
        JsonSerializer.Deserialize<MessageEnvelope>(deadLetter.Body.Span, MessagingJson.Options)!.EventId.Should().Be(envelope.EventId);
    }

    [Fact]
    public async Task MalformedMessage_IsDeadLetteredWithoutRetry()
    {
        await _channel.BasicPublishAsync(string.Empty, Queue, Encoding.UTF8.GetBytes("this is not an envelope"));

        var deadLetter = await EventuallyAsync(
            () => _channel.BasicGetAsync($"{Queue}.dlq", autoAck: true),
            message => message is not null,
            "the malformed message to reach the dead-letter queue");

        Encoding.UTF8.GetString(deadLetter!.Body.Span).Should().Be("this is not an envelope");
        Encoding.UTF8.GetString((byte[])deadLetter.BasicProperties.Headers![MessageHeaders.DeadLetterReason]!).Should().StartWith("Malformed message");
    }

    private async Task<bool> QueueExistsAsync()
    {
        try
        {
            await using var probe = await _connection.CreateChannelAsync();
            await probe.QueueDeclarePassiveAsync($"{Queue}.dlq");
            return true;
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            return false;
        }
    }
}
