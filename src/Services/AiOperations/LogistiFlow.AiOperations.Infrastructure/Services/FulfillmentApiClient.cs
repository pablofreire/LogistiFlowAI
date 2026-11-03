using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.AiOperations.Infrastructure.Services;

public sealed class FulfillmentApiClient(HttpClient httpClient, ILogger<FulfillmentApiClient> logger) : ServiceApiClient(httpClient, logger), IFulfillmentApi
{
    protected override string ServiceName => "Fulfillment";

    public Task<ServiceReply<FulfillmentJobView>> GetJobByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        GetAsync<FulfillmentJobView>($"api/fulfillment/jobs/by-order/{orderId}", cancellationToken);

    public Task<ServiceReply<FulfillmentMetricsView>> GetMetricsAsync(CancellationToken cancellationToken) =>
        GetAsync<FulfillmentMetricsView>("api/metrics/fulfillment", cancellationToken);
}
