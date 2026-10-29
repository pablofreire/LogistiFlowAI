using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Products.Create.Interfaces;

public interface ICreateProductUseCase : IUseCase<CreateProductRequest, ProductItem>;
