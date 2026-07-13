#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fistix.TaskManager.AiLayer.Tools;

/// <summary>
/// Canonical descriptions of todo management tools available for LLM function calling.
/// </summary>
public static class TodoToolDefinitions
{
    public const string CreateTodo = "create_todo";
    public const string UpdateTodo = "update_todo";
    public const string MarkComplete = "mark_complete";
    public const string SetPriority = "set_priority";
    public const string SearchTodos = "search_todos";
    public const string GetStatistics = "get_statistics";

    public static readonly HashSet<string> AllowedToolNames = new(StringComparer.OrdinalIgnoreCase)
    {
        CreateTodo,
        UpdateTodo,
        MarkComplete,
        SetPriority,
        SearchTodos,
        GetStatistics
    };

    public static string BuildCatalogForPrompt()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Available tools (use exact tool names):");
        builder.AppendLine("- create_todo(title: string, description: string, priority?: High|Medium|Low, dueDate?: ISO-8601 datetime, category?: string)");
        builder.AppendLine("- update_todo(id: guid, title?: string, description?: string, priority?: High|Medium|Low, status?: string, dueDate?: ISO-8601 datetime)");
        builder.AppendLine("- mark_complete(id: guid)");
        builder.AppendLine("- set_priority(id: guid, priority: High|Medium|Low)");
        builder.AppendLine("- search_todos(query: string)");
        builder.AppendLine("- get_statistics()");
        return builder.ToString();
    }

    public static bool IsAllowed(string? toolName) =>
        !string.IsNullOrWhiteSpace(toolName) && AllowedToolNames.Contains(toolName.Trim());

    public static string NormalizeName(string toolName) =>
        AllowedToolNames.First(n => n.Equals(toolName.Trim(), StringComparison.OrdinalIgnoreCase));
}
