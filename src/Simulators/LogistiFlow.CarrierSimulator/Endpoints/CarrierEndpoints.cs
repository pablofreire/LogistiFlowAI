using System.Globalization;
using LogistiFlow.CarrierSimulator.Booking;
using LogistiFlow.CarrierSimulator.Carriers;
using LogistiFlow.CarrierSimulator.Models;
using LogistiFlow.CarrierSimulator.Options;
using LogistiFlow.CarrierSimulator.Scenarios;
using Microsoft.Extensions.Options;

namespace LogistiFlow.CarrierSimulator.Endpoints;

/// <summary>The carrier API LogistiFlow integrates with: book a shipment, then read its tracking events.</summary>
public static class CarrierEndpoints
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static IEndpointRouteBuilder MapCarrierEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/shipments", CreateShipmentAsync);
        endpoints.MapGet("/v1/shipments/{trackingNumber}/events", GetTracking);
        return endpoints;
    }

    private static async Task<IResult> CreateShipmentAsync(
        HttpContext context,
        CreateShipmentRequest request,
        BookingStore store,
        IOptions<SimulatorOptions> options,
        TimeProvider timeProvider)
    {
        var idempotencyKey = context.Request.Headers[IdempotencyKeyHeader].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.Problem($"The {IdempotencyKeyHeader} header is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!CarrierCatalog.TryGet(request.CarrierCode, out var carrierName, out var transitDays))
        {
            return Results.Problem($"Unknown carrier '{request.CarrierCode}'.", statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        if (string.IsNullOrWhiteSpace(request.DestinationCity) || string.IsNullOrWhiteSpace(request.Reference))
        {
            return Results.Problem("Reference and destination city are required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var attempt = store.RegisterAttempt(idempotencyKey);
        var scenario = ScenarioCatalog.ForCity(request.DestinationCity);

        if (scenario == CarrierScenario.TemporaryFailure && attempt <= options.Value.TemporaryFailureCount)
        {
            return Results.Problem("Carrier booking service is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var now = timeProvider.GetUtcNow();
        var (booking, created) = store.GetOrCreate(idempotencyKey, sequence => new CarrierBooking(
            idempotencyKey,
            string.Create(CultureInfo.InvariantCulture, $"{request.CarrierCode}{sequence}"),
            request.CarrierCode,
            request.Reference,
            scenario,
            CarrierCatalog.AddBusinessDays(DateOnly.FromDateTime(now.UtcDateTime), transitDays),
            now,
            ScenarioCatalog.Timeline(scenario, carrierName, request.Origin, $"{request.DestinationCity}, {request.DestinationCountry}")));

        // The booking exists before the slow answer: a client that times out and retries with the same key must get
        // this booking back, not a second one.
        if (scenario == CarrierScenario.Timeout && attempt == 1)
        {
            await Task.Delay(options.Value.TimeoutDelay, timeProvider, context.RequestAborted);
        }

        var response = new CreateShipmentResponse(booking.TrackingNumber, booking.CarrierCode, booking.EstimatedDelivery, booking.BookedAt);
        return created
            ? Results.Created($"/v1/shipments/{booking.TrackingNumber}/events", response)
            : Results.Ok(response);
    }

    private static IResult GetTracking(string trackingNumber, BookingStore store, IOptions<SimulatorOptions> options, TimeProvider timeProvider)
    {
        if (!store.TryGetByTracking(trackingNumber, out var booking))
        {
            return Results.Problem($"No shipment with tracking number '{trackingNumber}'.", statusCode: StatusCodes.Status404NotFound);
        }

        var elapsed = timeProvider.GetUtcNow() - booking.BookedAt;
        var step = options.Value.StepInterval;
        var estimate = booking.EstimatedDelivery;
        var visible = new List<TrackingEventResponse>();
        for (var index = 0; index < booking.Timeline.Count; index++)
        {
            var scripted = booking.Timeline[index];
            var offset = step * scripted.Step;
            if (offset > elapsed)
            {
                break;
            }

            estimate = CarrierCatalog.AddBusinessDays(estimate, scripted.EstimateShiftDays);
            visible.Add(new TrackingEventResponse(
                string.Create(CultureInfo.InvariantCulture, $"{booking.TrackingNumber}-{index:D2}"),
                scripted.Code,
                scripted.Description,
                scripted.Location,
                booking.BookedAt + offset,
                estimate));
        }

        return Results.Ok(new TrackingResponse(booking.TrackingNumber, booking.CarrierCode, visible[^1].Code, estimate, visible));
    }
}
