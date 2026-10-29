using LogistiFlow.BuildingBlocks.Application.DependencyInjection;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Infrastructure.HealthChecks;
using LogistiFlow.BuildingBlocks.Infrastructure.Options;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Connections;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Migrations;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;

public static class BuildingBlocksInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// PostgreSQL access for one service: a pooled data source over the service's own database, the scoped
    /// session and unit of work, the migrator and the readiness check.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DapperConfiguration.Configure();

        services.AddBuildingBlocksApplication();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "Database:ConnectionString is required.")
            .ValidateOnStart();

        services.TryAddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return new NpgsqlDataSourceBuilder(options.ConnectionString).Build();
        });

        services.TryAddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.TryAddScoped<DbSession>();
        services.TryAddScoped<IDbSession>(provider => provider.GetRequiredService<DbSession>());
        services.TryAddScoped<IUnitOfWork, DbUnitOfWork>();
        services.TryAddSingleton<DatabaseMigrator>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

        return services;
    }
}
