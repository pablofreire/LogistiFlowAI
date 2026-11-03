using System.Reflection;
using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Options;
using LogistiFlow.AiOperations.Domain.Interfaces.Repositories;
using LogistiFlow.AiOperations.Infrastructure.Options;
using LogistiFlow.AiOperations.Infrastructure.Persistence.Readers;
using LogistiFlow.AiOperations.Infrastructure.Persistence.Repositories;
using LogistiFlow.AiOperations.Infrastructure.Providers;
using LogistiFlow.AiOperations.Infrastructure.Services;
using LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LogistiFlow.AiOperations.Infrastructure.DependencyInjection;

public static class AiOperationsInfrastructureServiceCollectionExtensions
{
    /// <summary>Assembly holding the AiOperations migrations (0601+).</summary>
    public static Assembly MigrationsAssembly => typeof(AiOperationsInfrastructureServiceCollectionExtensions).Assembly;

    /// <summary>
    /// Persistence, the provider and the typed service clients. <paramref name="configureServiceClient"/> adds to
    /// each client's pipeline (the API adds the handler that forwards the caller's token; tests swap the transport).
    /// </summary>
    public static IServiceCollection AddAiOperationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<string, IHttpClientBuilder>? configureServiceClient = null)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        services.AddOptions<CopilotOptions>()
            .Bind(configuration.GetSection(CopilotOptions.SectionName))
            .Validate(options => options.MaxToolRounds is >= 1 and <= 20, "Copilot:MaxToolRounds must be between 1 and 20.")
            .Validate(options => options.Timeout > TimeSpan.Zero, "Copilot:Timeout must be positive.")
            .ValidateOnStart();
        services.AddOptions<AiProviderOptions>().Bind(configuration.GetSection(AiProviderOptions.SectionName));
        services.AddOptions<OperationsServicesOptions>()
            .Bind(configuration.GetSection(OperationsServicesOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ICopilotInteractionRepository, CopilotInteractionRepository>();
        services.AddScoped<IAiOperationsQueries, AiOperationsQueryReader>();
        services.AddSingleton<ICopilotChatClientFactory, CopilotChatClientFactory>();

        AddServiceClient<IOrdersApi, OrdersApiClient>(services, "orders", options => options.Orders, configureServiceClient);
        AddServiceClient<IInventoryApi, InventoryApiClient>(services, "inventory", options => options.Inventory, configureServiceClient);
        AddServiceClient<IFulfillmentApi, FulfillmentApiClient>(services, "fulfillment", options => options.Fulfillment, configureServiceClient);
        AddServiceClient<IShippingApi, ShippingApiClient>(services, "shipping", options => options.Shipping, configureServiceClient);
        return services;
    }

    private static void AddServiceClient<TClient, TImplementation>(
        IServiceCollection services,
        string service,
        Func<OperationsServicesOptions, Uri> address,
        Action<string, IHttpClientBuilder>? configure)
        where TClient : class
        where TImplementation : class, TClient
    {
        // Reads only, so retries are safe; the copilot's own timeout bounds the whole question.
        var builder = services.AddHttpClient<TClient, TImplementation>((provider, client) =>
            client.BaseAddress = address(provider.GetRequiredService<IOptions<OperationsServicesOptions>>().Value));
        builder.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            });
        configure?.Invoke(service, builder);
    }
}
