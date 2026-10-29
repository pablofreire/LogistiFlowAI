using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Contracts;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Interfaces;

public interface ICancelFulfillmentForOrderUseCase : IUseCase<CancelFulfillmentForOrderRequest, CancelFulfillmentForOrderResponse>;
