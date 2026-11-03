using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Interfaces;

/// <summary>Read access to the Inventory service, as the calling user.</summary>
public interface IInventoryApi
{
    Task<ServiceReply<SkuAvailabilityView>> GetAvailabilityAsync(string sku, CancellationToken cancellationToken);

    Task<ServiceReply<InventoryMetricsView>> GetMetricsAsync(CancellationToken cancellationToken);
}
