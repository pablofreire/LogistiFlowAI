using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Inventory.Api.Controllers;

[Route("api/inventory/reservations")]
public sealed class ReservationsController : BaseApiController
{
    [HttpGet("by-order/{orderId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<ReservationDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByOrder(Guid orderId, [FromServices] IGetReservationByOrderUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetReservationByOrderRequest(orderId), cancellationToken));
}
