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

        string? warning = null;
        if (!string.Equals(created.Priority, normalizedPriority, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                created = await _api.UpdateTodoAsync(
                    created.ExternalId,
                    created.Title ?? title.Trim(),
                    created.Description ?? description?.Trim() ?? string.Empty,
                    created.DueDate ?? parsedDueDate,
                    normalizedPriority,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                warning = $"priority update failed: {ex.Message}";
            }
        }

        return JsonSerializer.Serialize(new
        {
            message = "Todo created",
            todo = created,
            warning
        }, JsonOptions);
    }

    [McpServerTool(Name = "update_todo"), Description("Updates an existing todo (title, description, priority, and/or due date). Omitted fields keep their current values.")]
    public async Task<string> UpdateTodo(
        [Description("Todo external id (GUID)")] string externalId,
        [Description("New title (optional)")] string? title,
        [Description("New description (optional)")] string? description,
        [Description("New priority: Low, Medium, or High (optional)")] string? priority,
        [Description("New due date in ISO-8601 format (optional)")] string? dueDate,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(externalId, out var id))
        {
            throw new ArgumentException("externalId must be a valid GUID.", nameof(externalId));
        }

        if (title is null && description is null && priority is null && dueDate is null)
        {
            throw new ArgumentException("Provide at least one field to update: title, description, priority, or dueDate.");
        }

        var existing = await _api.GetTodoAsync(id, cancellationToken);

        var newTitle = title is null ? (existing.Title ?? string.Empty) : title.Trim();
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            throw new ArgumentException("title cannot be empty.", nameof(title));
        }

        var newDescription = description is null
            ? (existing.Description ?? string.Empty)
            : description.Trim();

        var newPriority = priority is null
            ? (string.IsNullOrWhiteSpace(existing.Priority) ? "Medium" : existing.Priority!)
            : NormalizePriority(priority);

        DateTime newDueDate;
        if (dueDate is null)
        {
            newDueDate = existing.DueDate ?? DateTime.UtcNow.Date;
        }
        else if (!DateTime.TryParse(dueDate, out newDueDate))
        {
            throw new ArgumentException("dueDate must be a valid date (ISO-8601 preferred).", nameof(dueDate));
        }

        var updated = await _api.UpdateTodoAsync(
            id,
            newTitle,
            newDescription,
            newDueDate,
            newPriority,
            cancellationToken);

        return JsonSerializer.Serialize(new
        {
            message = "Todo updated",
            todo = updated
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

        var attempt = await _api.TrySemanticSearchAsync(query.Trim(), limit: 10, cancellationToken);
        if (!attempt.SoftFallback && attempt.Response?.Results is { Count: > 0 })
        {
            return JsonSerializer.Serialize(new
            {
                mode = "semantic",
                model = attempt.Response.Model,
                executionTimeMs = attempt.Response.ExecutionTimeMs,
                results = attempt.Response.Results
            }, JsonOptions);
        }

        var todos = await _api.GetTodosAsync(cancellationToken);
        var filtered = WorkloadAnalyzer.FilterByQuery(todos, query);

        return JsonSerializer.Serialize(new
        {
            mode = "keyword",
            count = filtered.Count,
            rateLimited = attempt.RateLimited ? true : (bool?)null,
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
