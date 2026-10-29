using LogistiFlow.BuildingBlocks.Messaging.DependencyInjection;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Hosting;
using LogistiFlow.Contracts.IntegrationEvents.Fulfillment;
using LogistiFlow.Contracts.IntegrationEvents.Orders;
using LogistiFlow.Shipping.Application.DependencyInjection;
using LogistiFlow.Shipping.Application.IntegrationEventHandlers;
using LogistiFlow.Shipping.Infrastructure.DependencyInjection;

if (HealthProbe.IsRequested(args))
{
    return await HealthProbe.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddLogistiFlowServiceDefaults("shipping-api");
builder.Services.AddShippingApplication();
builder.Services.AddShippingInfrastructure(builder.Configuration);
builder.Services.AddLogistiFlowMessaging(builder.Configuration, "shipping", handlers => handlers
    .Add<OrderCreatedEvent, OrderCreatedEventHandler>()
    .Add<FulfillmentShippedEvent, FulfillmentShippedEventHandler>());

var app = builder.Build();
app.UseLogistiFlowServiceDefaults();

return await app.RunWithCommandsAsync(
    args,
    MessagingServiceCollectionExtensions.MigrationsAssembly,
    ShippingInfrastructureServiceCollectionExtensions.MigrationsAssembly);
