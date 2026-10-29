using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Orders.Application.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;
using LogistiFlow.Orders.Domain.Errors;

namespace LogistiFlow.Orders.Application.UseCases.Orders.GetHistory.Implementations;

public sealed class GetOrderHistoryUseCase(IOrderQueries queries) : IGetOrderHistoryUseCase
{
    public async Task<Result<IReadOnlyList<OrderStatusChangeItem>>> ExecuteAsync(GetOrderHistoryRequest request, CancellationToken cancellationToken)
    {
        var history = await queries.GetHistoryAsync(request.OrderId, cancellationToken);
        return history is null ? OrderErrors.NotFound : Result.Success(history);
    }
}
