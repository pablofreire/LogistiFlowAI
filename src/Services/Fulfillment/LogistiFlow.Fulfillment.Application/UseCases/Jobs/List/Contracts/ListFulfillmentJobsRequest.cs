using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Contracts;

public sealed record ListFulfillmentJobsRequest(int? Page, int? PageSize, FulfillmentJobStatus? Status, string? WarehouseCode);
