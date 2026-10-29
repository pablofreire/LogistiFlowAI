using LogistiFlow.BuildingBlocks.Messaging.DependencyInjection;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Hosting;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Orders.Application.DependencyInjection;
using LogistiFlow.Orders.Application.IntegrationEventHandlers;
using LogistiFlow.Orders.Infrastructure.DependencyInjection;

if (HealthProbe.IsRequested(args))
{
    return await HealthProbe.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddLogistiFlowServiceDefaults("orders-api");
builder.Services.AddOrdersApplication();
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddLogistiFlowMessaging(builder.Configuration, "orders", handlers => handlers
    .Add<InventoryReservedEvent, InventoryReservedEventHandler>()
    .Add<InventoryReservationFailedEvent, InventoryReservationFailedEventHandler>()
    .Add<FulfillmentStartedEvent, FulfillmentStartedEventHandler>());

var app = builder.Build();
app.UseLogistiFlowServiceDefaults();

return await app.RunWithCommandsAsync(
    args,
    MessagingServiceCollectionExtensions.MigrationsAssembly,
    OrdersInfrastructureServiceCollectionExtensions.MigrationsAssembly);
