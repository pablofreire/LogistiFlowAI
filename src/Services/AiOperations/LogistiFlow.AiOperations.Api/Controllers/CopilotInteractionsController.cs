using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.AiOperations.Api.Controllers;

/// <summary>The copilot audit trail: every user's questions, the tools that ran and what they cost.</summary>
[Route("api/ai/interactions")]
public sealed class CopilotInteractionsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReviewCopilotAudit)]
    [ProducesResponseType<ApiResponse<PagedResult<CopilotInteractionSummary>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IListCopilotInteractionsUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListCopilotInteractionsRequest(page, pageSize), cancellationToken));
}
