#nullable enable

using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class CreateSprintCommand : IRequest<CreateSprintCommandResult>
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> TodoExternalIds { get; set; } = new();
    public string? Reasoning { get; set; }
}

public class CreateSprintCommandResult
{
    public SprintDto Payload { get; set; } = new();
}
