using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Contracts;

namespace LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Interfaces;

public interface IGetDashboardSummaryUseCase : IUseCase<GetDashboardSummaryRequest, GetDashboardSummaryResponse>;
