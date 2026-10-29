using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Products.List.Interfaces;

public interface IListProductsUseCase : IUseCase<ListProductsRequest, PagedResult<ProductItem>>;
