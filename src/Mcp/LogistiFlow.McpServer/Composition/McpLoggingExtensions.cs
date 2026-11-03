using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace LogistiFlow.McpServer.Composition;

public static class McpLoggingExtensions
{
    /// <summary>
    /// Serilog as compact JSON on standard error. Over stdio, standard output carries the MCP protocol, so nothing
    /// else may ever be written there: the default console logger is removed.
    /// </summary>
    public static HostApplicationBuilder AddMcpLogging(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((_, logger) => logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .MinimumLevel.Override("ModelContextProtocol", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LogistiFlow.McpServer")
            .WriteTo.Console(new CompactJsonFormatter(), standardErrorFromLevel: LogEventLevel.Verbose));
        return builder;
    }
}
