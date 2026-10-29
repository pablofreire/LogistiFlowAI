using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Orders.Application.Mappings;
using LogistiFlow.Orders.Application.UseCases.Orders.GetById.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.GetById.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;
using LogistiFlow.Orders.Domain.Errors;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;

namespace LogistiFlow.Orders.Application.UseCases.Orders.GetById.Implementations;

public sealed class GetOrderByIdUseCase(IOrderRepository repository) : IGetOrderByIdUseCase
{
    public async Task<Result<OrderDetails>> ExecuteAsync(GetOrderByIdRequest request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByPublicIdAsync(request.OrderId, cancellationToken);
        return order is null ? OrderErrors.NotFound : order.ToDetails();
    }
}
