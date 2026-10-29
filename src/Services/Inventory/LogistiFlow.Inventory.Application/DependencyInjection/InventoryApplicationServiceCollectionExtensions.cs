using FluentValidation;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.List.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Products.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.List.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Stock.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Implementations;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.Inventory.Application.DependencyInjection;

public static class InventoryApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ReserveInventoryUseCase>(ServiceLifetime.Singleton);

        services.AddScoped<IReserveInventoryUseCase, ReserveInventoryUseCase>();
        services.AddScoped<IReleaseInventoryUseCase, ReleaseInventoryUseCase>();
        services.AddScoped<IConsumeReservationUseCase, ConsumeReservationUseCase>();
        services.AddScoped<IGetReservationByOrderUseCase, GetReservationByOrderUseCase>();
        services.AddScoped<IListProductsUseCase, ListProductsUseCase>();
        services.AddScoped<ICreateProductUseCase, CreateProductUseCase>();
        services.AddScoped<IListWarehousesUseCase, ListWarehousesUseCase>();
        services.AddScoped<IListStockUseCase, ListStockUseCase>();
        services.AddScoped<IReceiveStockUseCase, ReceiveStockUseCase>();
        services.AddScoped<IGetSkuAvailabilityUseCase, GetSkuAvailabilityUseCase>();
        services.AddScoped<IGetInventoryMetricsUseCase, GetInventoryMetricsUseCase>();

        return services;
    }
}
