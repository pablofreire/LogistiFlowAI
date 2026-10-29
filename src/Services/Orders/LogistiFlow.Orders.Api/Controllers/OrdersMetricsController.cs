using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Orders.Api.Controllers;

[Route("api/metrics/orders")]
public sealed class OrdersMetricsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<GetOrdersMetricsResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSummary([FromServices] IGetOrdersMetricsUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetOrdersMetricsRequest(), cancellationToken));
}
