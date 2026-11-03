using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Interfaces;

/// <summary>Read access to the Fulfillment service, as the calling user.</summary>
public interface IFulfillmentApi
{
    Task<ServiceReply<FulfillmentJobView>> GetJobByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<ServiceReply<FulfillmentMetricsView>> GetMetricsAsync(CancellationToken cancellationToken);
}
