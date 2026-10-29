using LogistiFlow.Orders.Application.Services;
using LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;

namespace LogistiFlow.Orders.UnitTests.Application.Services;

public sealed class RequestHasherTests
{
    [Fact]
    public void Hash_IgnoresTheIdempotencyKeyCaseAndWhitespace()
    {
        var first = Request("key-a", " Acme ", "us", "sku-1");
        var second = Request("key-b", "Acme", "US", "SKU-1");

        RequestHasher.Hash(first).Should().Be(RequestHasher.Hash(second));
    }

    [Fact]
    public void Hash_DifferentQuantity_DiffersSoAReusedKeyIsDetected()
    {
        var original = Request("key", "Acme", "US", "SKU-1");
        var changed = original with { Lines = [new CreateOrderLineRequest("SKU-1", 3)] };

        RequestHasher.Hash(changed).Should().NotBe(RequestHasher.Hash(original));
    }

    [Fact]
    public void Hash_IsLowercaseSha256Hex()
    {
        RequestHasher.Hash(Request("key", "Acme", "US", "SKU-1")).Should().MatchRegex("^[0-9a-f]{64}$");
    }

    private static CreateOrderRequest Request(string key, string customer, string country, string sku) =>
        new(key, customer, "Austin", country, null, [new CreateOrderLineRequest(sku, 2)]);
}
