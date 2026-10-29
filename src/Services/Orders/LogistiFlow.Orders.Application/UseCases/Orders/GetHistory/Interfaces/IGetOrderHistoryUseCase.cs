using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;

namespace LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Interfaces;

public interface IGetOrderHistoryUseCase : IUseCase<GetOrderHistoryRequest, IReadOnlyList<OrderStatusChangeItem>>;
