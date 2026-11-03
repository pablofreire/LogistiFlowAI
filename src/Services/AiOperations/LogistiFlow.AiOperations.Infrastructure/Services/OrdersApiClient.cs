using LogistiFlow.AiOperations.Application.Interfaces;
using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;
using LogistiFlow.AiOperations.Domain.Enums;
using LogistiFlow.AiOperations.Infrastructure.Services.Contracts;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.AiOperations.Infrastructure.Services;

public sealed class OrdersApiClient(HttpClient httpClient, ILogger<OrdersApiClient> logger) : ServiceApiClient(httpClient, logger), IOrdersApi
{
    protected override string ServiceName => "Orders";

    public async Task<ServiceReply<OrderView>> FindByNumberAsync(string orderNumber, CancellationToken cancellationToken)
    {
        // The search matches number and customer name by prefix; the copilot needs the exact order.
        var search = await GetAsync<ServicePage<OrderSearchItemView>>($"api/orders?search={Uri.EscapeDataString(orderNumber)}&pageSize=20", cancellationToken);
        if (!search.IsOk)
        {
            return search.As<OrderView>();
        }

        var match = search.Data!.Items.FirstOrDefault(order => string.Equals(order.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? ServiceReply.Fail<OrderView>(CopilotToolCallOutcome.NotFound, $"Order {orderNumber} does not exist.")
            : await GetAsync<OrderView>($"api/orders/{match.Id}", cancellationToken);
    }

    public Task<ServiceReply<IReadOnlyList<StatusChangeView>>> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<StatusChangeView>>($"api/orders/{orderId}/history", cancellationToken);
}
