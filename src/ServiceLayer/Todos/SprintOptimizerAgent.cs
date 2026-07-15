#nullable enable

using Fistix.TaskManager.AiLayer.Agents;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

/// <summary>
/// Microsoft Agent Framework sprint planner: multi-step tool use then create_sprint.
/// Falls back to heuristic selection when the agent run fails.
/// </summary>
public class SprintOptimizerAgent
{
    private const string Instructions = """
        You are a sprint planning agent for a task manager.
        You MUST use tools — do not invent todo GUIDs.
        Suggested flow:
        1) search_incomplete_todos
        2) get_workload_stats and/or find_due_soon_todos
        3) propose_sprint_plan with a comma-separated list of real todo ids and brief reasoning
        4) create_sprint to persist the plan
        Prefer High priority, earlier due dates, and thematic grouping.
        Respect max task and duration constraints exposed by the tools.
        After tools finish, give a short final summary for the user.
        """;

    private readonly AiChatClientFactory _chatClientFactory;
    private readonly SprintPlanningTools _tools;
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ILogger<SprintOptimizerAgent> _logger;

    public SprintOptimizerAgent(
        AiChatClientFactory chatClientFactory,
        SprintPlanningTools tools,
        ITodoTaskRepository todoTaskRepository,
        ILogger<SprintOptimizerAgent> logger)
    {
        _chatClientFactory = chatClientFactory;
        _tools = tools;
        _todoTaskRepository = todoTaskRepository;
        _logger = logger;
    }

    public async Task<SprintOptimizationPlan> PlanAsync(
        Guid ownerId,
        int maxTasks,
        int durationDays,
        string? name,
        CancellationToken cancellationToken)
    {
        await _tools.ConfigureAsync(ownerId, maxTasks, durationDays, name, cancellationToken);

        try
        {
            var chatClient = _chatClientFactory.CreateChatClient();
            AIAgent agent = chatClient.AsAIAgent(
                instructions: Instructions,
                name: "SprintPlanningAgent",
                description: "Plans and creates an optimized sprint using todo tools.",
                tools:
                [
                    AIFunctionFactory.Create(_tools.SearchIncompleteTodos),
                    AIFunctionFactory.Create(_tools.GetWorkloadStats),
                    AIFunctionFactory.Create(_tools.FindDueSoonTodos),
                    AIFunctionFactory.Create(_tools.ProposeSprintPlan),
                    AIFunctionFactory.Create(_tools.CreateSprint)
                ]);

            var goal =
                $"Plan and create a sprint lasting {durationDays} days with at most {maxTasks} tasks. " +
                (string.IsNullOrWhiteSpace(name) ? "" : $"Use sprint name '{name.Trim()}'. ") +
                "Use your tools to inspect workload, propose a selection, then call create_sprint.";

            var response = await agent.RunAsync(goal, cancellationToken: cancellationToken);
            EnsureToolStepsPresent(response);

            if (_tools.CreatedSprintId.HasValue && _tools.SelectedTodos.Count > 0)
            {
                return BuildPlanFromTools(response.Text, includeCreated: true);
            }

            if (_tools.SelectedTodos.Count > 0)
            {
                _logger.LogWarning("MAF agent proposed tasks but did not create sprint; handler will persist.");
                return BuildPlanFromTools(response.Text, includeCreated: false);
            }

            _logger.LogWarning("MAF sprint agent did not select tasks; falling back to heuristic.");
        }
        catch (System.ClientModel.ClientResultException ex)
        {
            var body = TryReadErrorBody(ex);
            _logger.LogWarning(
                ex,
                "MAF sprint agent failed with HTTP {Status}; falling back to heuristic. Body: {Body}",
                ex.Status,
                body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MAF sprint agent failed; falling back to heuristic selection");
        }

        return await HeuristicFallbackAsync(ownerId, maxTasks, durationDays, cancellationToken);
    }

    private SprintOptimizationPlan BuildPlanFromTools(string? agentText, bool includeCreated) =>
        new()
        {
            SelectedTodos = _tools.SelectedTodos.ToList(),
            Reasoning = BuildReasoning(agentText, _tools.LastProposeReasoning),
            Steps = _tools.Steps.ToList(),
            CreatedSprintId = includeCreated ? _tools.CreatedSprintId : null,
            CreatedSprintName = includeCreated ? _tools.CreatedSprintName : null,
            CreatedStartDate = includeCreated ? _tools.CreatedStartDate : null,
            CreatedEndDate = includeCreated ? _tools.CreatedEndDate : null
        };

    private async Task<SprintOptimizationPlan> HeuristicFallbackAsync(
        Guid ownerId,
        int maxTasks,
        int durationDays,
        CancellationToken cancellationToken)
    {
        var todos = await _todoTaskRepository.GetByOwner(ownerId, cancellationToken);
        var selected = todos
            .Where(SprintPlanningTools.IsCandidate)
            .OrderBy(t => string.Equals(t.Priority, "High", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .Take(Math.Clamp(maxTasks, 1, 50))
            .ToList();

        var reasoning =
            $"Selected {selected.Count} high/medium tasks for a {durationDays}-day sprint " +
            "using priority and due-date ordering (agent unavailable or invalid tool outcome).";

        var steps = _tools.Steps.ToList();
        steps.Add(new AgentStepDto { ToolName = "heuristic_fallback", Summary = reasoning });

        return new SprintOptimizationPlan
        {
            SelectedTodos = selected,
            Reasoning = reasoning,
            Steps = steps
        };
    }

    private void EnsureToolStepsPresent(AgentResponse response)
    {
        if (response.Messages is null)
        {
            return;
        }

        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is not FunctionCallContent call || string.IsNullOrWhiteSpace(call.Name))
                {
                    continue;
                }

                if (!_tools.Steps.Any(s => string.Equals(s.ToolName, call.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    _tools.Steps.Add(new AgentStepDto
                    {
                        ToolName = call.Name,
                        Summary = "Invoked by agent."
                    });
                }
            }
        }
    }

    private static string BuildReasoning(string? agentText, string proposeReasoning)
    {
        if (!string.IsNullOrWhiteSpace(agentText))
        {
            return agentText.Trim();
        }

        return string.IsNullOrWhiteSpace(proposeReasoning)
            ? "Sprint planned by agent."
            : proposeReasoning;
    }

    private static string TryReadErrorBody(System.ClientModel.ClientResultException ex)
    {
        try
        {
            // Prefer explicit content when the SDK exposes it; otherwise fall back to message text.
            var contentProp = ex.GetType().GetProperty("Content");
            if (contentProp?.GetValue(ex) is BinaryData data)
            {
                var text = data.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Length > 2000 ? text[..2000] + "…" : text;
                }
            }
        }
        catch
        {
            // ignore reflection failures
        }

        return ex.Message;
    }
}

public class SprintOptimizationPlan
{
    public List<TodoTask> SelectedTodos { get; set; } = [];
    public string Reasoning { get; set; } = string.Empty;
    public List<AgentStepDto> Steps { get; set; } = [];
    public Guid? CreatedSprintId { get; set; }
    public string? CreatedSprintName { get; set; }
    public DateTime? CreatedStartDate { get; set; }
    public DateTime? CreatedEndDate { get; set; }
}
