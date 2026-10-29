using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Contracts;
using LogistiFlow.Fulfillment.Api.Controllers.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;
using LogistiFlow.Fulfillment.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Fulfillment.Api.Controllers;

[Route("api/fulfillment/jobs")]
public sealed class FulfillmentJobsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<PagedResult<FulfillmentJobSummary>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] FulfillmentJobStatus? status,
        [FromQuery] string? warehouseCode,
        [FromServices] IListFulfillmentJobsUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListFulfillmentJobsRequest(page, pageSize, status, warehouseCode), cancellationToken));

    [HttpGet("{jobId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<FulfillmentJobDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid jobId, [FromServices] IGetFulfillmentJobUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetFulfillmentJobRequest(jobId, null), cancellationToken));

    [HttpGet("by-order/{orderId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<FulfillmentJobDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByOrder(Guid orderId, [FromServices] IGetFulfillmentJobUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetFulfillmentJobRequest(null, orderId), cancellationToken));

    /// <summary>Picking is complete and the package is sealed.</summary>
    [HttpPost("{jobId:guid}/pack")]
    [Authorize(Policy = AuthorizationPolicies.OperateWarehouse)]
    [ProducesResponseType<ApiResponse<PerformFulfillmentJobActionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Pack(Guid jobId, [FromServices] IPerformFulfillmentJobActionUseCase useCase, CancellationToken cancellationToken) =>
        Perform(jobId, FulfillmentJobAction.Pack, null, useCase, cancellationToken);

    /// <summary>The package left the dock; Inventory consumes the stock and Shipping books a carrier.</summary>
    [HttpPost("{jobId:guid}/ship")]
    [Authorize(Policy = AuthorizationPolicies.OperateWarehouse)]
    [ProducesResponseType<ApiResponse<PerformFulfillmentJobActionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Ship(Guid jobId, [FromServices] IPerformFulfillmentJobActionUseCase useCase, CancellationToken cancellationToken) =>
        Perform(jobId, FulfillmentJobAction.Ship, null, useCase, cancellationToken);

    /// <summary>Puts the job on hold as Failed with the operator's description.</summary>
    [HttpPost("{jobId:guid}/report-issue")]
    [Authorize(Policy = AuthorizationPolicies.OperateWarehouse)]
    [ProducesResponseType<ApiResponse<PerformFulfillmentJobActionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> ReportIssue(
        Guid jobId,
        [FromBody] ReportFulfillmentIssueBody body,
        [FromServices] IPerformFulfillmentJobActionUseCase useCase,
        CancellationToken cancellationToken) =>
        Perform(jobId, FulfillmentJobAction.ReportIssue, body.Reason, useCase, cancellationToken);

    /// <summary>Sends a failed job back to picking.</summary>
    [HttpPost("{jobId:guid}/reprocess")]
    [Authorize(Policy = AuthorizationPolicies.OperateWarehouse)]
    [ProducesResponseType<ApiResponse<PerformFulfillmentJobActionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Reprocess(Guid jobId, [FromServices] IPerformFulfillmentJobActionUseCase useCase, CancellationToken cancellationToken) =>
        Perform(jobId, FulfillmentJobAction.Reprocess, null, useCase, cancellationToken);

    private Task<IActionResult> Perform(
        Guid jobId,
        FulfillmentJobAction action,
        string? reason,
        IPerformFulfillmentJobActionUseCase useCase,
        CancellationToken cancellationToken)
    {
        var operatorName = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? User.Identity?.Name ?? "unknown";
        return ExecuteAsync(() => useCase.ExecuteAsync(new PerformFulfillmentJobActionRequest(jobId, action, reason, operatorName), cancellationToken));
    }
}
