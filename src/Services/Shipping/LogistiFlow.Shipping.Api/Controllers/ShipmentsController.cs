using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.List.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.List.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;
using LogistiFlow.Shipping.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Shipping.Api.Controllers;

[Route("api/shipping/shipments")]
public sealed class ShipmentsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<PagedResult<ShipmentSummary>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] ShipmentStatus? status,
        [FromQuery] bool? delayedOnly,
        [FromServices] IListShipmentsUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListShipmentsRequest(page, pageSize, status, delayedOnly ?? false), cancellationToken));

    [HttpGet("{shipmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<ShipmentDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid shipmentId, [FromServices] IGetShipmentUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetShipmentRequest(shipmentId, null), cancellationToken));

    [HttpGet("by-order/{orderId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<ShipmentDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByOrder(Guid orderId, [FromServices] IGetShipmentUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetShipmentRequest(null, orderId), cancellationToken));

    /// <summary>Reprocesses a shipment whose carrier booking gave up.</summary>
    [HttpPost("{shipmentId:guid}/retry-booking")]
    [Authorize(Policy = AuthorizationPolicies.ManageShipments)]
    [ProducesResponseType<ApiResponse<ShipmentActionOutcome>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> RetryBooking(Guid shipmentId, [FromServices] IRetryShipmentBookingUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new RetryShipmentBookingRequest(shipmentId), cancellationToken));
}
