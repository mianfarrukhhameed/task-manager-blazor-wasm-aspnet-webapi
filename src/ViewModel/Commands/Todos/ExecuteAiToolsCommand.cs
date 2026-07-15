using MediatR;
using System.Collections.Generic;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class ExecuteAiToolsCommand : IRequest<ExecuteAiToolsCommandResult>
{
    public List<ProposedToolCallDto> ConfirmedCalls { get; set; } = new();
}

public class ExecuteAiToolsCommandResult
{
    public ExecuteAiToolsResponseDto Payload { get; set; } = new();
}

public class ExecuteAiToolsResponseDto
{
    public List<ToolExecutionResultDto> Results { get; set; } = new();
}

public class ToolExecutionResultDto
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
}
