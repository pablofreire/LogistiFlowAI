using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Hosting;
using LogistiFlow.Gateway.DependencyInjection;

if (HealthProbe.IsRequested(args))
{
    return await HealthProbe.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddLogistiFlowServiceDefaults("gateway");
builder.Services.AddGateway(builder.Configuration);

var app = builder.Build();
app.UseLogistiFlowServiceDefaults();
app.UseRateLimiter();
app.MapReverseProxy();

return await app.RunWithCommandsAsync(args, GatewayServiceCollectionExtensions.MigrationsAssembly);
