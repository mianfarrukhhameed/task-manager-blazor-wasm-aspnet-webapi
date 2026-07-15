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
    private readonly ISprintRepository _sprintRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly SprintOptimizerAgent _agent;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<OptimizeSprintCommandHandler> _logger;

    public OptimizeSprintCommandHandler(
        ISprintRepository sprintRepository,
        ICurrentUserService currentUserService,
        SprintOptimizerAgent agent,
        AiConfiguration aiConfig,
        ILogger<OptimizeSprintCommandHandler> logger)
    {
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

        _logger.LogInformation("Running MAF sprint planning agent for user {UserId}", userId);

        var plan = await _agent.PlanAsync(
            userId,
            command.MaxTasks,
            command.DurationDays,
            command.Name,
            cancellationToken);

        Guid sprintId;
        string sprintName;
        DateTime startDate;
        DateTime endDate;

        if (plan.CreatedSprintId.HasValue
            && plan.CreatedStartDate.HasValue
            && plan.CreatedEndDate.HasValue)
        {
            sprintId = plan.CreatedSprintId.Value;
            sprintName = plan.CreatedSprintName ?? $"Optimized Sprint {plan.CreatedStartDate:yyyy-MM-dd}";
            startDate = plan.CreatedStartDate.Value;
            endDate = plan.CreatedEndDate.Value;
        }
        else
        {
            startDate = DateTime.UtcNow.Date;
            endDate = startDate.AddDays(command.DurationDays);
            sprintName = string.IsNullOrWhiteSpace(command.Name)
                ? $"Optimized Sprint {startDate:yyyy-MM-dd}"
                : command.Name.Trim();

            var sprint = new Sprint
            {
                Name = sprintName,
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
            sprintId = sprint.ExternalId;
        }

        return new OptimizeSprintCommandResult
        {
            Payload = new OptimizeSprintResponseDto
            {
                SprintId = sprintId,
                Name = sprintName,
                StartDate = startDate,
                EndDate = endDate,
                Reasoning = plan.Reasoning,
                Steps = plan.Steps,
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
}
