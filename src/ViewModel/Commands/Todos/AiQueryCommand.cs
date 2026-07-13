using MediatR;
using System;
using System.Collections.Generic;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class AiQueryCommand : IRequest<AiQueryCommandResult>
{
    public string Question { get; set; } = string.Empty;
    /// <summary>week | project | workload</summary>
    public string Context { get; set; } = "workload";
}

public class AiQueryCommandResult
{
    public AiQueryResponseDto Payload { get; set; } = new();
}

public class AiQueryResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<Guid> Sources { get; set; } = new();
    public string Model { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}
