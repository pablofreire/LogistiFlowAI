using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Contracts;

namespace LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Interfaces;

public interface IGetFulfillmentMetricsUseCase : IUseCase<GetFulfillmentMetricsRequest, GetFulfillmentMetricsResponse>;
