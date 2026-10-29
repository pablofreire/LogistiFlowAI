using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.Shared;
using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Errors;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;

namespace LogistiFlow.Inventory.Application.UseCases.Products.Create.Implementations;

public sealed class CreateProductUseCase(
    IValidator<CreateProductRequest> validator,
    IProductRepository products,
    TimeProvider timeProvider) : ICreateProductUseCase
{
    public async Task<Result<ProductItem>> ExecuteAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        var product = ProductEntity.Create(request.Sku, request.Name, request.Description, timeProvider.GetUtcNow());
        if (!await products.TryAddAsync(product, cancellationToken))
        {
            return InventoryErrors.SkuAlreadyExists;
        }

        return new ProductItem(product.PublicId, product.Sku, product.Name, product.Description, 0, 0, 0);
    }
}
