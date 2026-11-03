using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.AiOperations.Infrastructure.Services;

public sealed class InventoryApiClient(HttpClient httpClient, ILogger<InventoryApiClient> logger) : ServiceApiClient(httpClient, logger), IInventoryApi
{
    protected override string ServiceName => "Inventory";

    public Task<ServiceReply<SkuAvailabilityView>> GetAvailabilityAsync(string sku, CancellationToken cancellationToken) =>
        GetAsync<SkuAvailabilityView>($"api/inventory/availability/{Uri.EscapeDataString(sku)}", cancellationToken);

    public Task<ServiceReply<InventoryMetricsView>> GetMetricsAsync(CancellationToken cancellationToken) =>
        GetAsync<InventoryMetricsView>("api/metrics/inventory", cancellationToken);
}
