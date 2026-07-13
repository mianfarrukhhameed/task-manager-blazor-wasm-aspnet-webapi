using MediatR;
using System.Collections.Generic;
using System.Text.Json;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class ProposeAiToolsCommand : IRequest<ProposeAiToolsCommandResult>
{
    public string Prompt { get; set; } = string.Empty;
}

public class ProposeAiToolsCommandResult
{
    public ProposeAiToolsResponseDto Payload { get; set; } = new();
}

public class ProposeAiToolsResponseDto
{
    public string Explanation { get; set; } = string.Empty;
    public List<ProposedToolCallDto> ProposedCalls { get; set; } = new();
    public string Model { get; set; } = string.Empty;
}

public class ProposedToolCallDto
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Arguments { get; set; } = new();
}
