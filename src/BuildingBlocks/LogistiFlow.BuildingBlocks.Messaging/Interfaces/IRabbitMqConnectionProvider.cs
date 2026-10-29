using RabbitMQ.Client;

namespace LogistiFlow.BuildingBlocks.Messaging.Interfaces;

/// <summary>One long-lived, auto-recovering AMQP connection per process; channels are created per use.</summary>
public interface IRabbitMqConnectionProvider
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
