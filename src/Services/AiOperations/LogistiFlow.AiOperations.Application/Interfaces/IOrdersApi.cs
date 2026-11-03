using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Interfaces;

/// <summary>Read access to the Orders service, as the calling user.</summary>
public interface IOrdersApi
{
    /// <summary>The order with exactly this number, or <c>NotFound</c>.</summary>
    Task<ServiceReply<OrderView>> FindByNumberAsync(string orderNumber, CancellationToken cancellationToken);

    Task<ServiceReply<IReadOnlyList<StatusChangeView>>> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken);
}
