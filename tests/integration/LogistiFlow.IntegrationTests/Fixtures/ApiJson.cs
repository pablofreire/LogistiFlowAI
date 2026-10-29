using System.Net.Http.Json;
using System.Text.Json;

namespace LogistiFlow.IntegrationTests.Fixtures;

/// <summary>Reads the <c>ApiResponse</c> envelope returned by every endpoint.</summary>
public static class ApiJson
{
    public static async Task<JsonElement> ReadEnvelopeAsync(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    public static async Task<JsonElement> ReadDataAsync(this HttpResponseMessage response)
    {
        var envelope = await response.ReadEnvelopeAsync();
        envelope.GetProperty("success").GetBoolean().Should().BeTrue(envelope.ToString());
        return envelope.GetProperty("data");
    }

    public static async Task<string> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        var envelope = await response.ReadEnvelopeAsync();
        envelope.GetProperty("success").GetBoolean().Should().BeFalse();
        return envelope.GetProperty("error").GetProperty("code").GetString()!;
    }

    public static Task<HttpResponseMessage> PostOrderAsync(this HttpClient client, object body, string? idempotencyKey = null, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders") { Content = JsonContent.Create(body) };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (correlationId is not null)
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        return client.SendAsync(request);
    }

    public static object OrderBody(
        string sku,
        int quantity,
        string customer = "Integration Test Retail",
        string city = "Austin",
        string country = "US",
        string? promisedDeliveryDate = "2026-12-01") => new
    {
        customerName = customer,
        destinationCity = city,
        destinationCountry = country,
        promisedDeliveryDate,
        lines = new[] { new { sku, quantity } },
    };
}
