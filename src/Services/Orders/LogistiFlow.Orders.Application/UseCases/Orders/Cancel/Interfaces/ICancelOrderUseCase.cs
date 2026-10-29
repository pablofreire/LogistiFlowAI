using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;

namespace LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Interfaces;

public interface ICancelOrderUseCase : IUseCase<CancelOrderRequest, OrderDetails>;
