using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Inventory.Api.Controllers.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Stock.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Inventory.Api.Controllers;

[Route("api/inventory")]
public sealed class StockController : BaseApiController
{
    [HttpGet("stock")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<PagedResult<StockPositionItem>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? warehouseCode,
        [FromQuery] string? sku,
        [FromQuery] bool lowStockOnly,
        [FromServices] IListStockUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListStockRequest(page, pageSize, warehouseCode, sku, lowStockOnly), cancellationToken));

    [HttpPost("stock/receipts")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventory)]
    [ProducesResponseType<ApiResponse<GetSkuAvailabilityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Receive([FromBody] ReceiveStockBody body, [FromServices] IReceiveStockUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ReceiveStockRequest(body.WarehouseCode, body.Sku, body.Quantity, body.Reference), cancellationToken));

    [HttpGet("availability/{sku}")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<GetSkuAvailabilityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetAvailability(string sku, [FromServices] IGetSkuAvailabilityUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetSkuAvailabilityRequest(sku), cancellationToken));
}
