using LogistiFlow.BuildingBlocks.Messaging.DependencyInjection;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Hosting;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Fulfillment.Application.DependencyInjection;
using LogistiFlow.Fulfillment.Application.IntegrationEventHandlers;
using LogistiFlow.Fulfillment.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogistiFlowServiceDefaults("fulfillment-api");
builder.Services.AddFulfillmentApplication();
builder.Services.AddFulfillmentInfrastructure(builder.Configuration);
builder.Services.AddLogistiFlowMessaging(builder.Configuration, "fulfillment", handlers => handlers
    .Add<InventoryReservedEvent, InventoryReservedEventHandler>()
    .Add<OrderCancelledEvent, OrderCancelledEventHandler>());

var app = builder.Build();
app.UseLogistiFlowServiceDefaults();

return await app.RunWithCommandsAsync(
    args,
    MessagingServiceCollectionExtensions.MigrationsAssembly,
    FulfillmentInfrastructureServiceCollectionExtensions.MigrationsAssembly);
