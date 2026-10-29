using LogistiFlow.BuildingBlocks.Application.Correlation.Interfaces;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Contracts;
using LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Gateway.Controllers;

[Route("api/dashboard")]
public sealed class DashboardController : BaseApiController
{
    /// <summary>Operations KPIs aggregated from every service, with the caller's own permissions.</summary>
    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<GetDashboardSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSummary(
        [FromServices] IGetDashboardSummaryUseCase useCase,
        [FromServices] ICorrelationContext correlation,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(
            new GetDashboardSummaryRequest(Request.Headers.Authorization.ToString(), correlation.CorrelationId),
            cancellationToken));
}
