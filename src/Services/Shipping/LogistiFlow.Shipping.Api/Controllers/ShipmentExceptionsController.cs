using System.IdentityModel.Tokens.Jwt;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Shipping.Api.Controllers.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Shared;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;
using LogistiFlow.Shipping.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Shipping.Api.Controllers;

[Route("api/shipping/exceptions")]
public sealed class ShipmentExceptionsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<PagedResult<ShipmentExceptionItem>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] ShipmentExceptionStatus? status,
        [FromQuery] ShipmentExceptionType? type,
        [FromServices] IListShipmentExceptionsUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListShipmentExceptionsRequest(page, pageSize, status, type), cancellationToken));

    [HttpPost("{exceptionId:guid}/resolve")]
    [Authorize(Policy = AuthorizationPolicies.ManageShipments)]
    [ProducesResponseType<ApiResponse<ShipmentActionOutcome>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Resolve(
        Guid exceptionId,
        [FromBody] ResolveShipmentExceptionBody body,
        [FromServices] IResolveShipmentExceptionUseCase useCase,
        CancellationToken cancellationToken)
    {
        var resolvedBy = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? User.Identity?.Name ?? "unknown";
        return ExecuteAsync(() => useCase.ExecuteAsync(new ResolveShipmentExceptionRequest(exceptionId, body.Resolution, resolvedBy), cancellationToken));
    }
}
