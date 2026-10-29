using System.Reflection;
using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Connections;
using LogistiFlow.BuildingBlocks.Messaging.Consuming;
using LogistiFlow.BuildingBlocks.Messaging.HealthChecks;
using LogistiFlow.BuildingBlocks.Messaging.Inbox;
using LogistiFlow.BuildingBlocks.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Options;
using LogistiFlow.BuildingBlocks.Messaging.Outbox;
using LogistiFlow.BuildingBlocks.Messaging.Publishing;
using LogistiFlow.BuildingBlocks.Messaging.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogistiFlow.BuildingBlocks.Messaging.DependencyInjection;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>Assembly holding the shared outbox and inbox migrations (00xx), applied to every service database.</summary>
    public static Assembly MigrationsAssembly => typeof(MessagingServiceCollectionExtensions).Assembly;

    /// <summary>
    /// Outbox writer and relay, inbox, RabbitMQ publisher and (when handlers are registered) the queue consumer.
    /// The relay and the consumer can be switched off through configuration, which tests use to drive them.
    /// </summary>
    public static IServiceCollection AddLogistiFlowMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<IntegrationEventHandlerRegistry>? configureHandlers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddOptions<MessagingOptions>().Configure(options => options.ServiceName = serviceName);
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddOptions<ConsumerOptions>().Bind(configuration.GetSection(ConsumerOptions.SectionName));

        var registry = new IntegrationEventHandlerRegistry(services);
        configureHandlers?.Invoke(registry);
        services.TryAddSingleton(registry);

        services.TryAddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.TryAddSingleton<OutboxRelay>();
        services.TryAddSingleton<IntegrationEventDispatcher>();
        services.TryAddScoped<IIntegrationEventOutbox, OutboxIntegrationEventWriter>();
        services.TryAddScoped<InboxStore>();
        services.TryAddSingleton<ConsumerReadiness>();

        if (configuration.GetValue($"{OutboxOptions.SectionName}:RelayEnabled", defaultValue: true))
        {
            services.AddHostedService<OutboxRelayWorker>();
        }

        if (registry.Registrations.Count > 0 && configuration.GetValue($"{ConsumerOptions.SectionName}:Enabled", defaultValue: true))
        {
            services.AddHostedService<RabbitMqConsumerWorker>();
            services.AddHealthChecks().AddCheck<ConsumerReadinessHealthCheck>("consumer-topology", tags: ["ready"]);
        }

        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["messaging"]);

        return services;
    }
}
