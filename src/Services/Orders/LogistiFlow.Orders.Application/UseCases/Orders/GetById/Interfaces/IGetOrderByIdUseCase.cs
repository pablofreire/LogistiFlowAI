using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.GetById.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;

namespace LogistiFlow.Orders.Application.UseCases.Orders.GetById.Interfaces;

public interface IGetOrderByIdUseCase : IUseCase<GetOrderByIdRequest, OrderDetails>;
