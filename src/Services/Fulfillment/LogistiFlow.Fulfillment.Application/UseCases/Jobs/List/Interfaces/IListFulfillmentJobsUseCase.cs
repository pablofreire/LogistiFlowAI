using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Interfaces;

public interface IListFulfillmentJobsUseCase : IUseCase<ListFulfillmentJobsRequest, PagedResult<FulfillmentJobSummary>>;
