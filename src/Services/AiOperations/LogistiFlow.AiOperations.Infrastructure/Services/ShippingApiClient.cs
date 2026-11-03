using System.Globalization;
using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Infrastructure.Services.Contracts;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.AiOperations.Infrastructure.Services;

public sealed class ShippingApiClient(HttpClient httpClient, ILogger<ShippingApiClient> logger) : ServiceApiClient(httpClient, logger), IShippingApi
{
    protected override string ServiceName => "Shipping";

    public Task<ServiceReply<ShipmentView>> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        GetAsync<ShipmentView>($"api/shipping/shipments/by-order/{orderId}", cancellationToken);

    public Task<ServiceReply<ShipmentView>> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        GetAsync<ShipmentView>($"api/shipping/shipments/{shipmentId}", cancellationToken);

    public async Task<ServiceReply<PageView<ShipmentListItemView>>> ListShipmentsAsync(bool delayedOnly, int pageSize, CancellationToken cancellationToken)
    {
        var path = string.Create(CultureInfo.InvariantCulture, $"api/shipping/shipments?pageSize={pageSize}{(delayedOnly ? "&delayedOnly=true" : string.Empty)}");
        return ToPage(await GetAsync<ServicePage<ShipmentListItemView>>(path, cancellationToken));
    }

    public async Task<ServiceReply<PageView<ShipmentExceptionView>>> ListExceptionsAsync(int pageSize, CancellationToken cancellationToken) =>
        ToPage(await GetAsync<ServicePage<ShipmentExceptionView>>(string.Create(CultureInfo.InvariantCulture, $"api/shipping/exceptions?pageSize={pageSize}"), cancellationToken));

    private static ServiceReply<PageView<T>> ToPage<T>(ServiceReply<ServicePage<T>> reply) =>
        reply.IsOk ? ServiceReply.Ok(new PageView<T>(reply.Data!.Items, reply.Data.TotalCount)) : reply.As<PageView<T>>();
}
