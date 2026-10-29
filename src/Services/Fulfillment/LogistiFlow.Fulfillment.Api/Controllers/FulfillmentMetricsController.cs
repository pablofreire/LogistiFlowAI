using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Fulfillment.Api.Controllers;

[Route("api/metrics/fulfillment")]
public sealed class FulfillmentMetricsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<GetFulfillmentMetricsResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSummary([FromServices] IGetFulfillmentMetricsUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetFulfillmentMetricsRequest(), cancellationToken));
}
