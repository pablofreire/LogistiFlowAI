using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Inventory.Api.Controllers;

[Route("api/metrics/inventory")]
public sealed class InventoryMetricsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<GetInventoryMetricsResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSummary([FromServices] IGetInventoryMetricsUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetInventoryMetricsRequest(), cancellationToken));
}
