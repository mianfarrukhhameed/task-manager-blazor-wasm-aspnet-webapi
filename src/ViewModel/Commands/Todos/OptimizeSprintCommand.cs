#nullable enable

using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class OptimizeSprintCommand : IRequest<OptimizeSprintCommandResult>
{
    public int MaxTasks { get; set; } = 12;
    public int DurationDays { get; set; } = 14;
    public string? Name { get; set; }
}

public class OptimizeSprintCommandResult
{
    public OptimizeSprintResponseDto Payload { get; set; } = new();
}

public class OptimizeSprintResponseDto
{
    public Guid SprintId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<SprintTaskSummaryDto> SelectedTasks { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    /// <summary>Ordered tool invocations from the Microsoft Agent Framework run (demo trail).</summary>
    public List<AgentStepDto> Steps { get; set; } = new();
}

public class AgentStepDto
{
    public string ToolName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
