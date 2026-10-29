using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Inventory.Api.Controllers;

[Route("api/inventory/warehouses")]
public sealed class WarehousesController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<IReadOnlyList<WarehouseItem>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List([FromServices] IListWarehousesUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListWarehousesRequest(), cancellationToken));
}
