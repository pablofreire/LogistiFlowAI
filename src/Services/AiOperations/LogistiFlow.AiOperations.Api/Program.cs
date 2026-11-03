using LogistiFlow.AiOperations.Api.Composition;
using LogistiFlow.AiOperations.Application.DependencyInjection;
using LogistiFlow.AiOperations.Infrastructure.DependencyInjection;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Hosting;

if (HealthProbe.IsRequested(args))
{
    return await HealthProbe.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddLogistiFlowServiceDefaults("aioperations-api");
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardCallerHeadersHandler>();
builder.Services.AddAiOperationsApplication();
builder.Services.AddAiOperationsInfrastructure(
    builder.Configuration,
    (_, client) => client.AddHttpMessageHandler<ForwardCallerHeadersHandler>());

var app = builder.Build();
app.UseLogistiFlowServiceDefaults();

return await app.RunWithCommandsAsync(args, AiOperationsInfrastructureServiceCollectionExtensions.MigrationsAssembly);
