using System.Text.Json.Serialization;
using LogistiFlow.BuildingBlocks.Infrastructure.Observability;
using LogistiFlow.BuildingBlocks.Web.Api;
using LogistiFlow.BuildingBlocks.Web.ErrorHandling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace LogistiFlow.BuildingBlocks.Web.Configuration;

public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Shared HTTP surface of every service: Serilog and OpenTelemetry, controllers with string enums,
    /// the <see cref="ApiResponse{T}"/> envelope for model-binding errors, the global exception handler,
    /// the OpenAPI document and JWT authentication with the permission policies.
    /// </summary>
    public static WebApplicationBuilder AddLogistiFlowServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddLogistiFlowObservability(
            serviceName,
            tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation(),
            metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());

        builder.Services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var details = context.ModelState
                        .Where(entry => entry.Value is { Errors.Count: > 0 })
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray(),
                            StringComparer.Ordinal);
                    var error = new ApiError("request.invalid", "The request is malformed.", details);
                    return new BadRequestObjectResult(ApiResponse.Fail(error, context.HttpContext.TraceIdentifier));
                };
            });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddLogistiFlowJwtAuthentication(builder.Configuration);

        return builder;
    }
}
