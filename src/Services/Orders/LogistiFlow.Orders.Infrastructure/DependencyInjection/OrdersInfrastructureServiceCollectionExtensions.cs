using System.Reflection;
using LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using LogistiFlow.Orders.Application.Interfaces;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;
using LogistiFlow.Orders.Infrastructure.Persistence.Readers;
using LogistiFlow.Orders.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.Orders.Infrastructure.DependencyInjection;

public static class OrdersInfrastructureServiceCollectionExtensions
{
    /// <summary>Assembly holding the Orders migrations (0101+).</summary>
    public static Assembly MigrationsAssembly => typeof(OrdersInfrastructureServiceCollectionExtensions).Assembly;

    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderQueries, OrderQueryReader>();
        return services;
    }
}
