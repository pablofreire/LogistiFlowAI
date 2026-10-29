using FluentValidation;
using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Implementations;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Implementations;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Implementations;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.GetById.Implementations;
using LogistiFlow.Orders.Application.UseCases.Orders.GetById.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Implementations;
using LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Implementations;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.Orders.Application.DependencyInjection;

public static class OrdersApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateOrderUseCase>(ServiceLifetime.Singleton);

        services.AddScoped<ICreateOrderUseCase, CreateOrderUseCase>();
        services.AddScoped<IGetOrderByIdUseCase, GetOrderByIdUseCase>();
        services.AddScoped<IListOrdersUseCase, ListOrdersUseCase>();
        services.AddScoped<ICancelOrderUseCase, CancelOrderUseCase>();
        services.AddScoped<IGetOrderHistoryUseCase, GetOrderHistoryUseCase>();
        services.AddScoped<IGetOrdersMetricsUseCase, GetOrdersMetricsUseCase>();
        services.AddScoped<OrderStatusEventApplier>();

        return services;
    }
}
