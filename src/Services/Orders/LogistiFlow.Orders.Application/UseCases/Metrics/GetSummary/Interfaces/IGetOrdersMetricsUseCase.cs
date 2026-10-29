using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;

namespace LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Interfaces;

public interface IGetOrdersMetricsUseCase : IUseCase<GetOrdersMetricsRequest, GetOrdersMetricsResponse>;
