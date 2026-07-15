#nullable enable

using Fistix.TaskManager.AiLayer.Agents;
using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

/// <summary>
/// Microsoft Agent Framework sprint planner.
/// Default: Analyst → Planner sequential workflow. Optional single-agent mode via Ai:Agents:WorkflowMode.
/// Falls back to heuristic selection when the agent run fails.
/// </summary>
public class SprintOptimizerAgent
{
    private const string AnalystInstructions = """
        You are the sprint workload Analyst for a task manager.
        You MUST use tools — do not invent todo GUIDs.
        Call search_incomplete_todos, get_workload_stats, and find_due_soon_todos.
        Then write a concise analysis for the Planner that includes:
        - Recommended todo external ids (comma-separated, real ids only)
        - Risks (overdue, overload, missing due-soon coverage)
        - Suggested sprint theme / grouping
        Do NOT call propose_sprint_plan or create_sprint — that is the Planner's job.
        """;

    private const string PlannerInstructions = """
        You are the sprint Planner for a task manager.
        You receive the Analyst's report. You MUST use tools — do not invent todo GUIDs.
        1) propose_sprint_plan with comma-separated real todo ids and brief reasoning
        2) create_sprint to persist
        Prefer High priority, earlier due dates, and the Analyst's recommendations.
        Respect max task and duration constraints exposed by the tools.
        After tools finish, give a short final summary for the user.
        """;

    private const string SingleAgentInstructions = """
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
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<SprintOptimizerAgent> _logger;

    public SprintOptimizerAgent(
        AiChatClientFactory chatClientFactory,
        SprintPlanningTools tools,
        ITodoTaskRepository todoTaskRepository,
        AiConfiguration aiConfig,
        ILogger<SprintOptimizerAgent> logger)
    {
        _chatClientFactory = chatClientFactory;
        _tools = tools;
        _todoTaskRepository = todoTaskRepository;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public async Task<SprintOptimizationPlan> PlanAsync(
        Guid ownerId,
        int maxTasks,
        int durationDays,
        string? name,
        CancellationToken cancellationToken)
    {
        var multi = IsMultiAgentMode();
        await _tools.ConfigureAsync(ownerId, maxTasks, durationDays, name, multi, cancellationToken);

        try
        {
            var chatClient = _chatClientFactory.CreateChatClient();
            var goal = BuildGoal(maxTasks, durationDays, name);

            AgentResponse response = multi
                ? await RunMultiAgentWorkflowAsync(chatClient, goal, cancellationToken)
                : await RunSingleAgentAsync(chatClient, goal, cancellationToken);

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

    private async Task<AgentResponse> RunMultiAgentWorkflowAsync(
        IChatClient chatClient,
        string goal,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running MAF Analyst → Planner sequential workflow");

        AIAgent analyst = chatClient.AsAIAgent(
            instructions: AnalystInstructions,
            name: "SprintAnalyst",
            description: "Analyzes incomplete todos and workload for sprint planning.",
            tools:
            [
                AIFunctionFactory.Create(_tools.SearchIncompleteTodos),
                AIFunctionFactory.Create(_tools.GetWorkloadStats),
                AIFunctionFactory.Create(_tools.FindDueSoonTodos)
            ]);

        AIAgent planner = chatClient.AsAIAgent(
            instructions: PlannerInstructions,
            name: "SprintPlanner",
            description: "Proposes and creates a sprint from the Analyst report.",
            tools:
            [
                AIFunctionFactory.Create(_tools.ProposeSprintPlan),
                AIFunctionFactory.Create(_tools.CreateSprint)
            ]);

        // chainOnlyAgentResponses: Planner gets Analyst's text brief, not the full tool transcript
        // (helps OpenAI-compat providers that struggle with long tool-history replays).
        var workflow = AgentWorkflowBuilder.BuildSequential(
            chainOnlyAgentResponses: true,
            agents: [analyst, planner]);

        AIAgent hosted = workflow.AsAIAgent(
            name: "SprintPlanningWorkflow",
            description: "Sequential Analyst → Planner sprint planning workflow.");

        _tools.Steps.Add(new AgentStepDto
        {
            AgentName = "Workflow",
            ToolName = "sequential_start",
            Summary = "Analyst → Planner"
        });

        return await hosted.RunAsync(goal, cancellationToken: cancellationToken);
    }

    private async Task<AgentResponse> RunSingleAgentAsync(
        IChatClient chatClient,
        string goal,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running MAF single sprint planning agent");

        AIAgent agent = chatClient.AsAIAgent(
            instructions: SingleAgentInstructions,
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

        return await agent.RunAsync(goal, cancellationToken: cancellationToken);
    }

    private bool IsMultiAgentMode()
    {
        var mode = _aiConfig.Agents?.WorkflowMode?.Trim() ?? "Multi";
        return !string.Equals(mode, "Single", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildGoal(int maxTasks, int durationDays, string? name) =>
        $"Plan and create a sprint lasting {durationDays} days with at most {maxTasks} tasks. " +
        (string.IsNullOrWhiteSpace(name) ? "" : $"Use sprint name '{name.Trim()}'. ") +
        "Analyst: inspect workload and recommend real todo ids. Planner: propose then create_sprint.";

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
        steps.Add(new AgentStepDto
        {
            AgentName = "Heuristic",
            ToolName = "heuristic_fallback",
            Summary = reasoning
        });

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
                        AgentName = _tools.UseMultiAgentLabels ? "Workflow" : "SprintAgent",
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
