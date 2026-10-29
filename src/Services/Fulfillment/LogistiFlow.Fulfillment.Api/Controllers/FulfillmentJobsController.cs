using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;
using LogistiFlow.Fulfillment.Domain.Enums;
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
}
