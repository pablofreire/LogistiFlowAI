using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;

namespace LogistiFlow.Orders.Application.UseCases.Orders.List.Interfaces;

public interface IListOrdersUseCase : IUseCase<ListOrdersRequest, PagedResult<OrderSummary>>;
