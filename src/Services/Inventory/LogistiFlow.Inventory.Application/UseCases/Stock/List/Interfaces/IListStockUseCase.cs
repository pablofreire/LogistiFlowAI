using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Stock.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Stock.List.Interfaces;

public interface IListStockUseCase : IUseCase<ListStockRequest, PagedResult<StockPositionItem>>;
