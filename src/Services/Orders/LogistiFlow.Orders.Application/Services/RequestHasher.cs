using System.Security.Cryptography;
using System.Text.Json;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;

namespace LogistiFlow.Orders.Application.Services;

/// <summary>
/// Fingerprint of a create-order request, excluding the idempotency key itself. Two requests with the same key
/// must have the same fingerprint, otherwise the client reused a key for a different order.
/// </summary>
public static class RequestHasher
{
    public static string Hash(CreateOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var canonical = new
        {
            customerName = request.CustomerName.Trim(),
            destinationCity = request.DestinationCity.Trim(),
            destinationCountry = request.DestinationCountry.Trim().ToUpperInvariant(),
            promisedDeliveryDate = request.PromisedDeliveryDate,
            lines = request.Lines.Select(line => new { sku = line.Sku.Trim().ToUpperInvariant(), quantity = line.Quantity }),
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
