using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Interfaces;

public interface IGetFulfillmentJobUseCase : IUseCase<GetFulfillmentJobRequest, FulfillmentJobDetails>;
