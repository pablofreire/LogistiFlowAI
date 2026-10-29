using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Interfaces;

public interface IGetInventoryMetricsUseCase : IUseCase<GetInventoryMetricsRequest, GetInventoryMetricsResponse>;
