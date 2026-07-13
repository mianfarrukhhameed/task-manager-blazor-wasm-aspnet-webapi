#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

/// <summary>
/// Simplified ReAct-style sprint planner: observe candidate todos, reason via LLM, act by returning a selection.
/// </summary>
public class SprintOptimizerAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILlmProviderService _llm;
    private readonly ILogger<SprintOptimizerAgent> _logger;

    public SprintOptimizerAgent(ILlmProviderService llm, ILogger<SprintOptimizerAgent> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    public async Task<SprintOptimizationPlan> PlanAsync(
        IReadOnlyList<TodoTask> candidates,
        int maxTasks,
        int durationDays,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return new SprintOptimizationPlan
            {
                SelectedTodos = [],
                Reasoning = "No high/medium priority incomplete tasks were available to plan a sprint."
            };
        }

        var cappedMax = Math.Min(maxTasks, candidates.Count);
        var prompt = BuildPrompt(candidates, cappedMax, durationDays);

        try
        {
            var raw = await _llm.GetCompletionAsync(prompt, cancellationToken);
            var parsed = ParseResponse(raw, candidates, cappedMax);
            if (parsed.SelectedTodos.Count > 0)
            {
                return parsed;
            }

            _logger.LogWarning("Sprint optimizer LLM returned no valid task ids; falling back to heuristic selection");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sprint optimizer LLM call failed; falling back to heuristic selection");
        }

        return HeuristicSelect(candidates, cappedMax, durationDays);
    }

    private static string BuildPrompt(IReadOnlyList<TodoTask> candidates, int maxTasks, int durationDays)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a sprint planning assistant.");
        sb.AppendLine($"Select up to {maxTasks} tasks for a {durationDays}-day sprint.");
        sb.AppendLine("Prefer High priority over Medium, earlier due dates, and thematic grouping.");
        sb.AppendLine("Only choose from the candidate list. Respond with JSON only:");
        sb.AppendLine("""{"selectedTaskIds":["guid",...],"reasoning":"brief explanation of grouping and balance"}""");
        sb.AppendLine();
        sb.AppendLine("Candidates:");

        foreach (var todo in candidates)
        {
            var title = PromptInputSanitizer.SanitizeAndTruncate(todo.Title, 200);
            var description = PromptInputSanitizer.SanitizeAndTruncate(todo.Description, 300);
            var category = PromptInputSanitizer.SanitizeAndTruncate(todo.Category, 80);
            sb.AppendLine(
                $"- id={todo.ExternalId}; priority={todo.Priority}; status={todo.Status}; " +
                $"due={todo.DueDate:yyyy-MM-dd}; category={category}; title={title}; description={description}");
        }

        return sb.ToString();
    }

    private SprintOptimizationPlan ParseResponse(string raw, IReadOnlyList<TodoTask> candidates, int maxTasks)
    {
        var json = ExtractJsonObject(raw);
        var parsed = JsonSerializer.Deserialize<LlmSprintPlanResponse>(json, JsonOptions);
        if (parsed?.SelectedTaskIds is null || parsed.SelectedTaskIds.Count == 0)
        {
            return new SprintOptimizationPlan { SelectedTodos = [], Reasoning = parsed?.Reasoning ?? string.Empty };
        }

        var byId = candidates.ToDictionary(t => t.ExternalId);
        var selected = new List<TodoTask>();
        foreach (var id in parsed.SelectedTaskIds.Distinct())
        {
            if (byId.TryGetValue(id, out var todo))
            {
                selected.Add(todo);
            }

            if (selected.Count >= maxTasks)
            {
                break;
            }
        }

        return new SprintOptimizationPlan
        {
            SelectedTodos = selected,
            Reasoning = string.IsNullOrWhiteSpace(parsed.Reasoning)
                ? "Selected tasks based on model recommendation."
                : parsed.Reasoning.Trim()
        };
    }

    private static SprintOptimizationPlan HeuristicSelect(
        IReadOnlyList<TodoTask> candidates,
        int maxTasks,
        int durationDays)
    {
        var selected = candidates
            .OrderBy(t => PriorityRank(t.Priority))
            .ThenBy(t => t.DueDate)
            .Take(maxTasks)
            .ToList();

        return new SprintOptimizationPlan
        {
            SelectedTodos = selected,
            Reasoning =
                $"Selected {selected.Count} high/medium tasks for a {durationDays}-day sprint " +
                "using priority and due-date ordering (LLM unavailable or invalid response)."
        };
    }

    private static int PriorityRank(string? priority) =>
        string.Equals(priority, "High", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

    private static string ExtractJsonObject(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var fenced = Regex.Match(trimmed, @"```(?:json)?\s*(\{[\s\S]*?\})\s*```", RegexOptions.IgnoreCase);
        if (fenced.Success)
        {
            return fenced.Groups[1].Value;
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return trimmed[start..(end + 1)];
        }

        return trimmed;
    }

    private sealed class LlmSprintPlanResponse
    {
        public List<Guid> SelectedTaskIds { get; set; } = [];
        public string? Reasoning { get; set; }
    }
}

public class SprintOptimizationPlan
{
    public List<TodoTask> SelectedTodos { get; set; } = [];
    public string Reasoning { get; set; } = string.Empty;
}
