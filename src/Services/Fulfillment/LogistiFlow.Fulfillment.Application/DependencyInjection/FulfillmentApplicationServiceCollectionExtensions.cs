using LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Implementations;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Implementations;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Implementations;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Implementations;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Implementations;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiFlow.Fulfillment.Application.DependencyInjection;

public static class FulfillmentApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFulfillmentApplication(this IServiceCollection services)
    {
        services.AddScoped<IStartFulfillmentUseCase, StartFulfillmentUseCase>();
        services.AddScoped<ICancelFulfillmentForOrderUseCase, CancelFulfillmentForOrderUseCase>();
        services.AddScoped<IListFulfillmentJobsUseCase, ListFulfillmentJobsUseCase>();
        services.AddScoped<IGetFulfillmentJobUseCase, GetFulfillmentJobUseCase>();
        services.AddScoped<IGetFulfillmentMetricsUseCase, GetFulfillmentMetricsUseCase>();
        return services;
    }
}
