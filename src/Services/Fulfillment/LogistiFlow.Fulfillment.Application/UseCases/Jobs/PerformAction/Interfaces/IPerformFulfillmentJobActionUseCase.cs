using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Contracts;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Interfaces;

public interface IPerformFulfillmentJobActionUseCase : IUseCase<PerformFulfillmentJobActionRequest, PerformFulfillmentJobActionResponse>;
