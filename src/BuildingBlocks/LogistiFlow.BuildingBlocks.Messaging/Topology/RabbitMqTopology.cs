using System.Globalization;
using RabbitMQ.Client;

namespace LogistiFlow.BuildingBlocks.Messaging.Topology;

/// <summary>
/// Broker topology. Every service publishes to one durable topic exchange. Each consuming service owns:
/// <list type="bullet">
/// <item><c>{service}.integration-events</c>, bound to the routing keys it handles;</item>
/// <item><c>{queue}.retry.{n}</c> tiers with a fixed TTL that dead-letter back into the main queue (backoff);</item>
/// <item><c>{queue}.dlq</c> for messages that exhausted their attempts or cannot be deserialized.</item>
/// </list>
/// Declarations are idempotent, so every instance declares on start-up.
/// </summary>
public static class RabbitMqTopology
{
    public static Task DeclareExchangeAsync(IChannel channel, string exchange, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        return channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
    }

    public static async Task DeclareConsumerQueuesAsync(
        IChannel channel,
        string exchange,
        string queue,
        IEnumerable<string> routingKeys,
        IReadOnlyList<TimeSpan> retryDelays,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(routingKeys);
        ArgumentNullException.ThrowIfNull(retryDelays);

        await DeclareExchangeAsync(channel, exchange, cancellationToken);
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        foreach (var routingKey in routingKeys)
        {
            await channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken: cancellationToken);
        }

        for (var tier = 0; tier < retryDelays.Count; tier++)
        {
            var arguments = new Dictionary<string, object?>
            {
                ["x-message-ttl"] = (int)retryDelays[tier].TotalMilliseconds,
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = queue,
            };
            await channel.QueueDeclareAsync(RetryQueue(queue, tier), durable: true, exclusive: false, autoDelete: false, arguments, cancellationToken: cancellationToken);
        }

        await channel.QueueDeclareAsync(DeadLetterQueue(queue), durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
    }

    public static string RetryQueue(string queue, int tier) => string.Create(CultureInfo.InvariantCulture, $"{queue}.retry.{tier + 1}");

    public static string DeadLetterQueue(string queue) => $"{queue}.dlq";
}
