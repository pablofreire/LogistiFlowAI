using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Create.Interfaces;

public interface ICreateOrderUseCase : IUseCase<CreateOrderRequest, CreateOrderResponse>;
