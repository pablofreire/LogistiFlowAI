using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Inventory.Api.Controllers.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.Create.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Inventory.Api.Controllers;

[Route("api/inventory/products")]
public sealed class ProductsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<PagedResult<ProductItem>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        [FromServices] IListProductsUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListProductsRequest(page, pageSize, search), cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageInventory)]
    [ProducesResponseType<ApiResponse<ProductItem>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateProductBody body, [FromServices] ICreateProductUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new CreateProductRequest(body.Sku, body.Name, body.Description), cancellationToken), StatusCodes.Status201Created);
}
