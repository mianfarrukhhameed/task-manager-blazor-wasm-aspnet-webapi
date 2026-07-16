using System.ComponentModel;
using System.Text.Json;
using Fistix.TaskManager.McpServer.Api;
using Fistix.TaskManager.McpServer.Services;
using ModelContextProtocol.Server;

namespace Fistix.TaskManager.McpServer.Resources;

[McpServerResourceType]
public sealed class TodoMcpResources
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly TaskManagerApiClient _api;

    public TodoMcpResources(TaskManagerApiClient api)
    {
        _api = api;
    }

    [McpServerResource(UriTemplate = "taskmanager://todos", Name = "todos", MimeType = "application/json")]
    [Description("All todos for the authenticated user from GET /api/todos.")]
    public async Task<string> GetTodos(CancellationToken cancellationToken)
    {
        var todos = await _api.GetTodosAsync(cancellationToken);
        return JsonSerializer.Serialize(new { count = todos.Count, todos }, JsonOptions);
    }

    [McpServerResource(UriTemplate = "taskmanager://statistics", Name = "statistics", MimeType = "application/json")]
    [Description("Computed productivity summary derived from the user's todos.")]
    public async Task<string> GetStatistics(CancellationToken cancellationToken)
    {
        var todos = await _api.GetTodosAsync(cancellationToken);
        var summary = WorkloadAnalyzer.Analyze(todos);
        return JsonSerializer.Serialize(summary, JsonOptions);
    }
}
