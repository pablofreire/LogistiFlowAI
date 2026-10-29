using LogistiFlow.BuildingBlocks.Application.Correlation.Implementations;
using LogistiFlow.BuildingBlocks.Application.Correlation.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogistiFlow.BuildingBlocks.Application.DependencyInjection;

public static class BuildingBlocksApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICorrelationContext, CorrelationContext>();
        return services;
    }
}
