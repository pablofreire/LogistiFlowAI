using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Shipping.Api.Controllers;

[Route("api/metrics/shipping")]
public sealed class ShippingMetricsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<GetShippingMetricsResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSummary([FromServices] IGetShippingMetricsUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetShippingMetricsRequest(), cancellationToken));
}
