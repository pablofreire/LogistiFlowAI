using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;
using LogistiFlow.Orders.Domain.Entities;

namespace LogistiFlow.Orders.Application.Mappings;

/// <summary>Manual mapping from the aggregate to API models and event contracts.</summary>
public static class OrderMappings
{
    public static OrderDetails ToDetails(this OrderEntity order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderDetails(
            order.PublicId,
            order.OrderNumber,
            order.CustomerName,
            order.DestinationCity,
            order.DestinationCountry,
            order.PromisedDeliveryDate,
            order.Status,
            order.StatusReason,
            order.Lines.Select(line => new OrderLineDetails(line.LineNumber, line.Sku, line.Quantity)).ToList(),
            order.CreatedAt,
            order.UpdatedAt);
    }

    public static IReadOnlyList<OrderLineItem> ToLineItems(this OrderEntity order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.Lines.Select(line => new OrderLineItem(line.Sku, line.Quantity)).ToList();
    }
}
