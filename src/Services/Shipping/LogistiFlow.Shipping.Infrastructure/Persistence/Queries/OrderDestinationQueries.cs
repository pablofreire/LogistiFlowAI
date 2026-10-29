namespace LogistiFlow.Shipping.Infrastructure.Persistence.Queries;

internal static class OrderDestinationQueries
{
    public const string Select = """
        SELECT order_id, order_number, customer_name, destination_city, destination_country, promised_delivery_date
        FROM shipping.order_destinations
        WHERE order_id = @OrderId
        """;

    public const string InsertIfMissing = """
        INSERT INTO shipping.order_destinations (order_id, order_number, customer_name, destination_city, destination_country,
                                                 promised_delivery_date)
        VALUES (@OrderId, @OrderNumber, @CustomerName, @DestinationCity, @DestinationCountry, CAST(@PromisedDeliveryDate AS date))
        ON CONFLICT (order_id) DO NOTHING
        """;
}
