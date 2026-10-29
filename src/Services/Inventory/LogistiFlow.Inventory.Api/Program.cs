using LogistiFlow.BuildingBlocks.Messaging.DependencyInjection;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Hosting;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Inventory.Application.DependencyInjection;
using LogistiFlow.Inventory.Application.IntegrationEventHandlers;
using LogistiFlow.Inventory.Infrastructure.DependencyInjection;

if (HealthProbe.IsRequested(args))
{
    return await HealthProbe.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddLogistiFlowServiceDefaults("inventory-api");
builder.Services.AddInventoryApplication();
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddLogistiFlowMessaging(builder.Configuration, "inventory", handlers => handlers
    .Add<OrderCreatedEvent, OrderCreatedEventHandler>()
    .Add<OrderCancelledEvent, OrderCancelledEventHandler>());

var app = builder.Build();
app.UseLogistiFlowServiceDefaults();

return await app.RunWithCommandsAsync(
    args,
    MessagingServiceCollectionExtensions.MigrationsAssembly,
    InventoryInfrastructureServiceCollectionExtensions.MigrationsAssembly);
