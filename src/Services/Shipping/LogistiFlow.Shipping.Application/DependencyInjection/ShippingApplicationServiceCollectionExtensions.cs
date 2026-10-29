using FluentValidation;
using LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.List.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Shipments.List.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Implementations;
using LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.Shipping.Application.DependencyInjection;

public static class ShippingApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddShippingApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ResolveShipmentExceptionUseCase>(ServiceLifetime.Singleton);

        services.AddScoped<IRecordOrderDestinationUseCase, RecordOrderDestinationUseCase>();
        services.AddScoped<ICreateShipmentUseCase, CreateShipmentUseCase>();
        services.AddScoped<IBookShipmentUseCase, BookShipmentUseCase>();
        services.AddScoped<IPollShipmentTrackingUseCase, PollShipmentTrackingUseCase>();
        services.AddScoped<IRetryShipmentBookingUseCase, RetryShipmentBookingUseCase>();
        services.AddScoped<IListShipmentsUseCase, ListShipmentsUseCase>();
        services.AddScoped<IGetShipmentUseCase, GetShipmentUseCase>();
        services.AddScoped<IListShipmentExceptionsUseCase, ListShipmentExceptionsUseCase>();
        services.AddScoped<IResolveShipmentExceptionUseCase, ResolveShipmentExceptionUseCase>();
        services.AddScoped<IGetShippingMetricsUseCase, GetShippingMetricsUseCase>();
        return services;
    }
}
