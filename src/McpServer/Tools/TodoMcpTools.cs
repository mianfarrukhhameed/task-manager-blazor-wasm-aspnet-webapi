using System.ComponentModel;
using System.Text.Json;
using Fistix.TaskManager.McpServer.Api;
using Fistix.TaskManager.McpServer.Services;
using ModelContextProtocol.Server;

namespace Fistix.TaskManager.McpServer.Tools;

[McpServerToolType]
public sealed class TodoMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly TaskManagerApiClient _api;

    public TodoMcpTools(TaskManagerApiClient api)
    {
        _api = api;
    }

    [McpServerTool(Name = "create_todo"), Description("Creates a new todo task via the Task Manager API.")]
    public async Task<string> CreateTodo(
        [Description("Short task title")] string title,
        [Description("Task description")] string description,
        [Description("Priority: Low, Medium, or High")] string priority,
        [Description("Due date in ISO-8601 format (e.g. 2026-07-20)")] string dueDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("title is required.", nameof(title));
        }

        if (!DateTime.TryParse(dueDate, out var parsedDueDate))
        {
            throw new ArgumentException("dueDate must be a valid date (ISO-8601 preferred).", nameof(dueDate));
        }

        var normalizedPriority = NormalizePriority(priority);

        var created = await _api.CreateTodoAsync(
            title.Trim(),
            description?.Trim() ?? string.Empty,
            parsedDueDate,
            cancellationToken);

        if (!string.Equals(created.Priority, normalizedPriority, StringComparison.OrdinalIgnoreCase))
        {
            created = await _api.UpdateTodoAsync(created, normalizedPriority, cancellationToken);
        }

        return JsonSerializer.Serialize(new
        {
            message = "Todo created",
            todo = created
        }, JsonOptions);
    }

    [McpServerTool(Name = "search_todos"), Description("Searches todos by natural language. Uses semantic search when available, otherwise filters the todo list.")]
    public async Task<string> SearchTodos(
        [Description("Search query")] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("query is required.", nameof(query));
        }

        var semantic = await _api.TrySemanticSearchAsync(query.Trim(), limit: 10, cancellationToken);
        if (semantic?.Results is { Count: > 0 })
        {
            return JsonSerializer.Serialize(new
            {
                mode = "semantic",
                model = semantic.Model,
                executionTimeMs = semantic.ExecutionTimeMs,
                results = semantic.Results
            }, JsonOptions);
        }

        var todos = await _api.GetTodosAsync(cancellationToken);
        var filtered = WorkloadAnalyzer.FilterByQuery(todos, query);

        return JsonSerializer.Serialize(new
        {
            mode = "keyword",
            count = filtered.Count,
            results = filtered
        }, JsonOptions);
    }

    [McpServerTool(Name = "analyze_workload"), Description("Analyzes the current todo workload: counts by priority/status, overdue items, and items due this week.")]
    public async Task<string> AnalyzeWorkload(CancellationToken cancellationToken)
    {
        var todos = await _api.GetTodosAsync(cancellationToken);
        var summary = WorkloadAnalyzer.Analyze(todos);
        return JsonSerializer.Serialize(summary, JsonOptions);
    }

    private static string NormalizePriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            return "Medium";
        }

        return priority.Trim().ToLowerInvariant() switch
        {
            "low" => "Low",
            "medium" => "Medium",
            "high" => "High",
            _ => throw new ArgumentException("priority must be Low, Medium, or High.", nameof(priority))
        };
    }
}
