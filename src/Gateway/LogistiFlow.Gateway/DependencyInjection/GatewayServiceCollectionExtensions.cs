using System.Reflection;
using System.Threading.RateLimiting;
using FluentValidation;
using LogistiFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.Gateway.Clients;
using LogistiFlow.Gateway.Interfaces;
using LogistiFlow.Gateway.Options;
using LogistiFlow.Gateway.Persistence.Repositories;
using LogistiFlow.Gateway.Persistence.Seed;
using LogistiFlow.Gateway.Proxy;
using LogistiFlow.Gateway.Security;
using LogistiFlow.Gateway.UseCases.Auth.Login.Implementations;
using LogistiFlow.Gateway.UseCases.Auth.Login.Interfaces;
using LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Implementations;
using LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Interfaces;

namespace LogistiFlow.Gateway.DependencyInjection;

public static class GatewayServiceCollectionExtensions
{
    /// <summary>Assembly holding the identity migrations (0401+).</summary>
    public static Assembly MigrationsAssembly => typeof(GatewayServiceCollectionExtensions).Assembly;

    public static IServiceCollection AddGateway(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);
        services.AddValidatorsFromAssemblyContaining<LoginUseCase>(ServiceLifetime.Singleton);

        services.AddOptions<DemoSeedOptions>().Bind(configuration.GetSection(DemoSeedOptions.SectionName));
        services.AddOptions<LoginRateLimitOptions>().Bind(configuration.GetSection(LoginRateLimitOptions.SectionName));
        var downstream = configuration.GetSection(DownstreamServicesOptions.SectionName).Get<DownstreamServicesOptions>() ?? new DownstreamServicesOptions();
        services.AddOptions<DownstreamServicesOptions>().Bind(configuration.GetSection(DownstreamServicesOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDemoDataSeeder, IdentityDemoDataSeeder>();
        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IGetDashboardSummaryUseCase, GetDashboardSummaryUseCase>();
        services.AddSingleton<IServiceMetricsClient, ServiceMetricsClient>();

        AddMetricsClient(services, GetDashboardSummaryUseCase.OrdersService, downstream.Orders);
        AddMetricsClient(services, GetDashboardSummaryUseCase.InventoryService, downstream.Inventory);
        AddMetricsClient(services, GetDashboardSummaryUseCase.FulfillmentService, downstream.Fulfillment);
        AddMetricsClient(services, GetDashboardSummaryUseCase.ShippingService, downstream.Shipping);

        var (routes, clusters) = ProxyConfiguration.Build(downstream);
        services.AddReverseProxy().LoadFromMemory(routes, clusters);

        var rateLimit = configuration.GetSection(LoginRateLimitOptions.SectionName).Get<LoginRateLimitOptions>() ?? new LoginRateLimitOptions();
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(LoginRateLimitOptions.PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = rateLimit.PermitLimit, Window = rateLimit.Window, QueueLimit = 0 }));
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var error = new ApiError("auth.rate_limited", "Too many sign-in attempts. Try again later.");
                await context.HttpContext.Response.WriteAsJsonAsync(ApiResponse.Fail(error, context.HttpContext.TraceIdentifier), cancellationToken);
            };
        });

        return services;
    }

    private static void AddMetricsClient(IServiceCollection services, string name, Uri baseAddress)
    {
        services.AddHttpClient(name, client => client.BaseAddress = baseAddress)
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(6);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            });
    }
}
