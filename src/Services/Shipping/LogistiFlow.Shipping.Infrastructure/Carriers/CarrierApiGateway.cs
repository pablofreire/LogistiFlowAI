using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Models;
using LogistiFlow.Shipping.Domain.Models;
using LogistiFlow.Shipping.Infrastructure.Carriers.Contracts;
using Polly.Timeout;

namespace LogistiFlow.Shipping.Infrastructure.Carriers;

/// <summary>
/// HTTP client for the carrier API. Every carrier-side problem (5xx, 4xx, timeout, connection refused, malformed body)
/// becomes a failed result with a readable reason; only cancellation by the host propagates.
/// </summary>
public sealed class CarrierApiGateway(HttpClient client) : ICarrierGateway
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public async Task<CarrierBookingResult> BookAsync(CarrierBookingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri("v1/shipments", UriKind.Relative))
        {
            Content = JsonContent.Create(new CarrierShipmentBody(
                request.CarrierCode,
                request.Reference,
                request.Origin,
                request.DestinationCity,
                request.DestinationCountry)),
        };
        message.Headers.Add(IdempotencyKeyHeader, request.IdempotencyKey);

        try
        {
            using var response = await client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CarrierBookingResult.Failed(Describe(response));
            }

            var reply = await response.Content.ReadFromJsonAsync<CarrierShipmentReply>(cancellationToken);
            return reply is { TrackingNumber: { Length: > 0 } tracking, EstimatedDelivery: { } estimate }
                ? CarrierBookingResult.Booked(tracking, estimate)
                : CarrierBookingResult.Failed("The carrier answered without a tracking number or estimate.");
        }
        catch (Exception exception) when (IsCarrierFailure(exception, cancellationToken))
        {
            return CarrierBookingResult.Failed(Describe(exception));
        }
    }

    public async Task<CarrierTrackingResult> GetTrackingAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        try
        {
            using var response = await client.GetAsync(
                new Uri($"v1/shipments/{Uri.EscapeDataString(trackingNumber)}/events", UriKind.Relative),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CarrierTrackingResult.Failed(Describe(response));
            }

            var reply = await response.Content.ReadFromJsonAsync<CarrierTrackingReply>(cancellationToken);
            var events = (reply?.Events ?? [])
                .Where(e => !string.IsNullOrWhiteSpace(e.EventId) && !string.IsNullOrWhiteSpace(e.Code))
                .Select(e => new CarrierTrackingEvent(e.EventId!, e.Code!, e.Description ?? e.Code!, e.Location ?? string.Empty, e.OccurredAt, e.EstimatedDelivery))
                .ToList();
            return CarrierTrackingResult.Read(events);
        }
        catch (Exception exception) when (IsCarrierFailure(exception, cancellationToken))
        {
            return CarrierTrackingResult.Failed(Describe(exception));
        }
    }

    private static bool IsCarrierFailure(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        HttpRequestException or TimeoutRejectedException or JsonException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };

    private static string Describe(HttpResponseMessage response) =>
        string.Create(CultureInfo.InvariantCulture, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

    private static string Describe(Exception exception) => exception switch
    {
        TimeoutRejectedException or OperationCanceledException => "The carrier did not answer in time.",
        HttpRequestException => $"The carrier could not be reached: {exception.Message}",
        _ => $"The carrier sent an unreadable answer: {exception.Message}",
    };
}
