using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.Security;
using LogistiFlow.Orders.Api.Controllers.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.GetById.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.GetById.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;
using LogistiFlow.Orders.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogistiFlow.Orders.Api.Controllers;

[Route("api/orders")]
public sealed class OrdersController : BaseApiController
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string IdempotentReplayedHeader = "Idempotent-Replayed";

    /// <summary>
    /// Creates an order. Requires an <c>Idempotency-Key</c> header: repeating the request with the same key and
    /// body returns the original order (200, <c>Idempotent-Replayed: true</c>); the same key with a different body
    /// returns 409.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageOrders)]
    [ProducesResponseType<ApiResponse<OrderDetails>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<OrderDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] CreateOrderBody body,
        [FromServices] ICreateOrderUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problem(Error.Validation("request.idempotency_key_required", $"The {IdempotencyKeyHeader} header is required."));
        }

        var request = new CreateOrderRequest(
            idempotencyKey,
            body.CustomerName,
            body.DestinationCity,
            body.DestinationCountry,
            body.PromisedDeliveryDate,
            body.Lines?.Select(line => new CreateOrderLineRequest(line.Sku, line.Quantity)).ToList() ?? []);

        var result = await useCase.ExecuteAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return Problem(result.Error);
        }

        if (result.Value.Replayed)
        {
            Response.Headers[IdempotentReplayedHeader] = "true";
            return Envelope(result.Value.Order, StatusCodes.Status200OK);
        }

        Response.Headers.Location = $"/api/orders/{result.Value.Order.Id}";
        return Envelope(result.Value.Order, StatusCodes.Status201Created);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<PagedResult<OrderSummary>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] OrderStatus? status,
        [FromQuery] string? search,
        [FromServices] IListOrdersUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new ListOrdersRequest(page, pageSize, status, search), cancellationToken));

    [HttpGet("{orderId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<OrderDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid orderId, [FromServices] IGetOrderByIdUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetOrderByIdRequest(orderId), cancellationToken));

    [HttpGet("{orderId:guid}/history")]
    [Authorize(Policy = AuthorizationPolicies.ReadOperations)]
    [ProducesResponseType<ApiResponse<IReadOnlyList<OrderStatusChangeItem>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetHistory(Guid orderId, [FromServices] IGetOrderHistoryUseCase useCase, CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new GetOrderHistoryRequest(orderId), cancellationToken));

    [HttpPost("{orderId:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ManageOrders)]
    [ProducesResponseType<ApiResponse<OrderDetails>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Cancel(
        Guid orderId,
        [FromBody] CancelOrderBody body,
        [FromServices] ICancelOrderUseCase useCase,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => useCase.ExecuteAsync(new CancelOrderRequest(orderId, body.Reason), cancellationToken));
}
