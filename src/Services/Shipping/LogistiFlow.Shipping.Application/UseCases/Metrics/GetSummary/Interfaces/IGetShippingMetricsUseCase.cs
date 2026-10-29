using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Contracts;

namespace LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Interfaces;

public interface IGetShippingMetricsUseCase : IUseCase<GetShippingMetricsRequest, GetShippingMetricsResponse>;
