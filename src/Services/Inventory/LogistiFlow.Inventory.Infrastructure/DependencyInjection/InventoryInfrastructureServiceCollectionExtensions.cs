using System.Reflection;
using LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;
using LogistiFlow.Inventory.Infrastructure.Persistence.Readers;
using LogistiFlow.Inventory.Infrastructure.Persistence.Repositories;
using LogistiFlow.Inventory.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.Inventory.Infrastructure.DependencyInjection;

public static class InventoryInfrastructureServiceCollectionExtensions
{
    /// <summary>Assembly holding the Inventory migrations (0201+).</summary>
    public static Assembly MigrationsAssembly => typeof(InventoryInfrastructureServiceCollectionExtensions).Assembly;

    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryQueries, InventoryQueryReader>();
        services.AddScoped<IDemoDataSeeder, InventoryDemoDataSeeder>();
        return services;
    }
}
