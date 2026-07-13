#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace Fistix.TaskManager.AiLayer.Tools;

/// <summary>
/// Semantic Kernel plugin definitions for todo tools.
/// Functions return structured intended actions only — no direct DB access.
/// Execution happens via IToolExecutor / MediatR in the ServiceLayer.
/// </summary>
public sealed class TodoManagementPlugin
{
    [KernelFunction(TodoToolDefinitions.CreateTodo)]
    [Description("Create a new todo task from title, description, and optional metadata.")]
    public Task<string> CreateTodoAsync(
        [Description("Task title")] string title,
        [Description("Task description")] string description,
        [Description("Priority: High, Medium, or Low")] string? priority = null,
        [Description("Due date in ISO-8601 format")] string? dueDate = null,
        [Description("Optional category")] string? category = null)
    {
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            action = TodoToolDefinitions.CreateTodo,
            title,
            description,
            priority,
            dueDate,
            category
        }));
    }

    [KernelFunction(TodoToolDefinitions.UpdateTodo)]
    [Description("Update an existing todo by id with any provided fields.")]
    public Task<string> UpdateTodoAsync(
        [Description("Todo external id (GUID)")] Guid id,
        [Description("New title")] string? title = null,
        [Description("New description")] string? description = null,
        [Description("New priority")] string? priority = null,
        [Description("New status")] string? status = null,
        [Description("New due date ISO-8601")] string? dueDate = null)
    {
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            action = TodoToolDefinitions.UpdateTodo,
            id,
            title,
            description,
            priority,
            status,
            dueDate
        }));
    }

    [KernelFunction(TodoToolDefinitions.MarkComplete)]
    [Description("Mark a todo as completed.")]
    public Task<string> MarkCompleteAsync([Description("Todo external id (GUID)")] Guid id)
    {
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            action = TodoToolDefinitions.MarkComplete,
            id
        }));
    }

    [KernelFunction(TodoToolDefinitions.SetPriority)]
    [Description("Set the priority of a todo.")]
    public Task<string> SetPriorityAsync(
        [Description("Todo external id (GUID)")] Guid id,
        [Description("Priority: High, Medium, or Low")] string priority)
    {
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            action = TodoToolDefinitions.SetPriority,
            id,
            priority
        }));
    }

    [KernelFunction(TodoToolDefinitions.SearchTodos)]
    [Description("Search todos by natural language query.")]
    public Task<string> SearchTodosAsync([Description("Search query")] string query)
    {
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            action = TodoToolDefinitions.SearchTodos,
            query
        }));
    }

    [KernelFunction(TodoToolDefinitions.GetStatistics)]
    [Description("Get aggregate statistics for the current user's todos.")]
    public Task<string> GetStatisticsAsync()
    {
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            action = TodoToolDefinitions.GetStatistics
        }));
    }
}
