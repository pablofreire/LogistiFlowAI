using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Contracts;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Interfaces;

public interface IStartFulfillmentUseCase : IUseCase<StartFulfillmentRequest, StartFulfillmentResponse>;
