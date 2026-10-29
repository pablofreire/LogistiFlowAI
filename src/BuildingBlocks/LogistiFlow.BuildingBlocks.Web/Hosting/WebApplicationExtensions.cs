using System.Reflection;
using LogistiFlow.BuildingBlocks.Infrastructure.Options;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Migrations;
using LogistiFlow.BuildingBlocks.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LogistiFlow.BuildingBlocks.Web.Hosting;

public static class WebApplicationExtensions
{
    public const string MigrateCommand = "--migrate";
    public const string SeedDemoCommand = "--seed-demo";

    /// <summary>Middleware order and the anonymous health endpoints shared by every service.</summary>
    public static WebApplication UseLogistiFlowServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapOpenApi().AllowAnonymous();

        // live: the process answers. ready: its own database answers. /health: everything, including the broker.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
        }).AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Runs the web host, or — when started with <c>--migrate</c> — applies the service's migrations, optionally
    /// seeds demo data (<c>--seed-demo</c>) and exits. Containers use the second form as an init step.
    /// </summary>
    public static async Task<int> RunWithCommandsAsync(this WebApplication app, string[] args, params Assembly[] migrationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(args);

        if (!args.Contains(MigrateCommand, StringComparer.Ordinal))
        {
            await app.RunAsync();
            return 0;
        }

        var connectionString = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString;
        var result = app.Services.GetRequiredService<DatabaseMigrator>().Migrate(connectionString, migrationAssemblies);
        if (!result.Successful)
        {
            return 1;
        }

        if (args.Contains(SeedDemoCommand, StringComparer.Ordinal))
        {
            await using var scope = app.Services.CreateAsyncScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<IDemoDataSeeder>())
            {
                await seeder.SeedAsync(CancellationToken.None);
            }
        }

        return 0;
    }
}
