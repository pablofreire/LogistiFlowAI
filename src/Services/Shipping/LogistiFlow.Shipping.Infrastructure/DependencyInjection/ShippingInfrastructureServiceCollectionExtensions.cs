using System.Reflection;
using LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Options;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Infrastructure.Carriers;
using LogistiFlow.Shipping.Infrastructure.Options;
using LogistiFlow.Shipping.Infrastructure.Persistence.Readers;
using LogistiFlow.Shipping.Infrastructure.Persistence.Repositories;
using LogistiFlow.Shipping.Infrastructure.Persistence.WorkQueues;
using LogistiFlow.Shipping.Infrastructure.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace LogistiFlow.Shipping.Infrastructure.DependencyInjection;

public static class ShippingInfrastructureServiceCollectionExtensions
{
    /// <summary>Assembly holding the Shipping migrations (0501+).</summary>
    public static Assembly MigrationsAssembly => typeof(ShippingInfrastructureServiceCollectionExtensions).Assembly;

    public static IServiceCollection AddShippingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        services.AddOptions<ShippingWorkerOptions>().Bind(configuration.GetSection(ShippingWorkerOptions.SectionName));
        services.AddOptions<CarrierApiOptions>().Bind(configuration.GetSection(CarrierApiOptions.SectionName));

        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IOrderDestinationRepository, OrderDestinationRepository>();
        services.AddScoped<IShipmentWorkQueue, ShipmentWorkQueue>();
        services.AddScoped<IShippingQueries, ShippingQueryReader>();

        // Only a per-call timeout here. Retries are durable and per shipment (BookShipmentUseCase), so a retry storm
        // inside one HTTP call would hide carrier failures that operators need to see.
        services.AddHttpClient<ICarrierGateway, CarrierApiGateway>((provider, client) =>
            {
                client.BaseAddress = provider.GetRequiredService<IOptions<CarrierApiOptions>>().Value.BaseAddress;
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler("carrier-api", (pipeline, context) =>
                pipeline.AddTimeout(context.ServiceProvider.GetRequiredService<IOptions<CarrierApiOptions>>().Value.Timeout));

        if (configuration.GetValue($"{ShippingWorkerOptions.SectionName}:Enabled", defaultValue: true))
        {
            services.AddHostedService<CarrierBookingWorker>();
            services.AddHostedService<TrackingPollingWorker>();
        }

        return services;
    }
}
