using LogistiFlow.BuildingBlocks.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LogistiFlow.BuildingBlocks.Messaging.Connections;

public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> rabbitOptions, IOptions<MessagingOptions> messagingOptions)
    : IRabbitMqConnectionProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            var options = rabbitOptions.Value;
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName = messagingOptions.Value.ServiceName,
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _gate.Dispose();
    }
}
