using Fistix.TaskManager.McpServer.Api;
using Fistix.TaskManager.McpServer.Configuration;
using Fistix.TaskManager.McpServer.Resources;
using Fistix.TaskManager.McpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var options = McpServerOptions.FromEnvironment();
options.Validate();

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(console =>
{
    // MCP stdio uses stdout for protocol messages; keep logs on stderr.
    console.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton(options);
builder.Services.AddHttpClient<TaskManagerApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<TodoMcpTools>()
    .WithResources<TodoMcpResources>();

await builder.Build().RunAsync();
