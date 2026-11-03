using LogistiFlow.McpServer.Composition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// A local MCP server over stdio. Configuration comes from environment variables (see GatewayOptions).
var builder = Host.CreateApplicationBuilder(args);
builder.AddMcpLogging();
builder.Services.AddLogistiFlowMcpServer(builder.Configuration).WithStdioServerTransport();

await builder.Build().RunAsync();
