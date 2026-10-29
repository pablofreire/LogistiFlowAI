using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Observability;

public static class ObservabilityHostApplicationBuilderExtensions
{
    /// <summary>
    /// Structured logging (Serilog, compact JSON in containers) and OpenTelemetry traces and metrics.
    /// Telemetry is exported over OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured
    /// (for example to an OpenTelemetry Collector feeding Prometheus and Grafana).
    /// </summary>
    public static IHostApplicationBuilder AddLogistiFlowObservability(
        this IHostApplicationBuilder builder,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var useJson = builder.Configuration.GetValue("Logging:Json", defaultValue: false);
        builder.Services.AddSerilog((_, logger) =>
        {
            logger.ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName);

            if (useJson)
            {
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
            else
            {
                logger.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture);
            }
        });

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource("LogistiFlow.*").AddNpgsql();
                configureTracing?.Invoke(tracing);
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("LogistiFlow.*").AddRuntimeInstrumentation();
                configureMetrics?.Invoke(metrics);
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter();
                }
            });

        return builder;
    }
}
