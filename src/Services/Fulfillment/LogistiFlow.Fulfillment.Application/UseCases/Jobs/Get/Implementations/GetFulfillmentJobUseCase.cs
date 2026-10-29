using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Fulfillment.Application.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;
using LogistiFlow.Fulfillment.Domain.Errors;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Implementations;

public sealed class GetFulfillmentJobUseCase(IFulfillmentQueries queries) : IGetFulfillmentJobUseCase
{
    public async Task<Result<FulfillmentJobDetails>> ExecuteAsync(GetFulfillmentJobRequest request, CancellationToken cancellationToken)
    {
        var job = request switch
        {
            { JobId: { } jobId, OrderId: null } => await queries.GetByIdAsync(jobId, cancellationToken),
            { JobId: null, OrderId: { } orderId } => await queries.GetByOrderIdAsync(orderId, cancellationToken),
            _ => null,
        };

        return job is null ? FulfillmentErrors.JobNotFound : job;
    }
}
