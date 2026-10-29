using System.Reflection;
using LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using LogistiFlow.Fulfillment.Application.Interfaces;
using LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;
using LogistiFlow.Fulfillment.Infrastructure.Persistence.Readers;
using LogistiFlow.Fulfillment.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.Fulfillment.Infrastructure.DependencyInjection;

public static class FulfillmentInfrastructureServiceCollectionExtensions
{
    /// <summary>Assembly holding the Fulfillment migrations (0301+).</summary>
    public static Assembly MigrationsAssembly => typeof(FulfillmentInfrastructureServiceCollectionExtensions).Assembly;

    public static IServiceCollection AddFulfillmentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);
        services.AddScoped<IFulfillmentJobRepository, FulfillmentJobRepository>();
        services.AddScoped<IFulfillmentQueries, FulfillmentQueryReader>();
        return services;
    }
}
