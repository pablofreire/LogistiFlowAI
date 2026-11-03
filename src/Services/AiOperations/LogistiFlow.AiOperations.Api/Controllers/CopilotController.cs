using System.IdentityModel.Tokens.Jwt;
using LogistiFlow.AiOperations.Api.Controllers.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Interfaces;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Contracts;
using LogistiFlow.AiOperations.Application.UseCases.Copilot.GetStatus.Interfaces;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Configuration;
using LogistiFlow.BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.AiOperations.Api.Controllers;

[Route("api/ai/copilot")]
public sealed class CopilotController : BaseApiController
{
    [HttpGet("status")]
    [Authorize(Policy = AuthorizationPolicies.UseCopilot)]
    [ProducesResponseType<ApiResponse<GetCopilotStatusResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetStatus([FromServices] IGetCopilotStatusUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetCopilotStatusRequest(), cancellationToken));

    [HttpPost("ask")]
    [Authorize(Policy = AuthorizationPolicies.UseCopilot)]
    [ProducesResponseType<ApiResponse<AskCopilotResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> Ask([FromBody] AskCopilotBody body, [FromServices] IAskCopilotUseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? User.Identity?.Name ?? "unknown";
        var role = User.FindFirst(JwtAuthenticationServiceCollectionExtensions.RoleClaimType)?.Value ?? "unknown";
        return ExecuteAsync(() => useCase.ExecuteAsync(new AskCopilotRequest(body.Question ?? string.Empty, body.ConversationId, email, role), cancellationToken));
    }
}
