#nullable enable

using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.Core.Exceptions;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class OptimizeSprintCommandHandler : IRequestHandler<OptimizeSprintCommand, OptimizeSprintCommandResult>
{
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly SprintOptimizerAgent _agent;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<OptimizeSprintCommandHandler> _logger;

    public OptimizeSprintCommandHandler(
        ITodoTaskRepository todoTaskRepository,
        ISprintRepository sprintRepository,
        ICurrentUserService currentUserService,
        SprintOptimizerAgent agent,
        AiConfiguration aiConfig,
        ILogger<OptimizeSprintCommandHandler> logger)
    {
        _todoTaskRepository = todoTaskRepository;
        _sprintRepository = sprintRepository;
        _currentUserService = currentUserService;
        _agent = agent;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public async Task<OptimizeSprintCommandResult> Handle(
        OptimizeSprintCommand command,
        CancellationToken cancellationToken)
    {
        if (!_aiConfig.Features.EnableAgents)
        {
            throw new FeatureDisabledException("AI sprint optimizer agent");
        }

        var userId = TodoAccessGuard.RequireCurrentUserId(_currentUserService);
        var todos = await _todoTaskRepository.GetByOwner(userId, cancellationToken);
        var candidates = todos
            .Where(IsCandidate)
            .OrderBy(t => string.Equals(t.Priority, "High", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ToList();

        _logger.LogInformation(
            "Sprint optimizer found {CandidateCount} candidate todos for user {UserId}",
            candidates.Count,
            userId);

        var plan = await _agent.PlanAsync(candidates, command.MaxTasks, command.DurationDays, cancellationToken);

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(command.DurationDays);
        var name = string.IsNullOrWhiteSpace(command.Name)
            ? $"Optimized Sprint {startDate:yyyy-MM-dd}"
            : command.Name.Trim();

        var sprint = new Sprint
        {
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            Reasoning = plan.Reasoning
        };
        sprint.GenerateNewExternalId();

        foreach (var todo in plan.SelectedTodos)
        {
            sprint.SprintTodos.Add(new SprintTodo { TodoId = todo.Id });
        }

        await _sprintRepository.Create(sprint, cancellationToken);

        return new OptimizeSprintCommandResult
        {
            Payload = new OptimizeSprintResponseDto
            {
                SprintId = sprint.ExternalId,
                Name = sprint.Name,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Reasoning = plan.Reasoning,
                SelectedTasks = plan.SelectedTodos.Select(t => new SprintTaskSummaryDto
                {
                    ExternalId = t.ExternalId,
                    Title = t.Title,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    Category = t.Category
                }).ToList()
            }
        };
    }

    private static bool IsCandidate(TodoTask todo)
    {
        if (string.Equals(todo.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(todo.Priority, "High", StringComparison.OrdinalIgnoreCase)
            || string.Equals(todo.Priority, "Medium", StringComparison.OrdinalIgnoreCase);
    }
}
