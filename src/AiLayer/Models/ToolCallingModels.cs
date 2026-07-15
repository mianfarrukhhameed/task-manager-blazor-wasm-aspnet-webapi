#nullable enable

using System.Collections.Generic;
using System.Text.Json;

namespace Fistix.TaskManager.AiLayer.Models;

public sealed class ProposedToolCall
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Arguments { get; set; } = new();
}

public sealed class ToolExecutionOutcome
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
}

public sealed class ToolProposalPipelineRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public sealed class ToolProposalPipelineResult
{
    public string Explanation { get; set; } = string.Empty;
    public IReadOnlyList<ProposedToolCall> ProposedCalls { get; set; } = [];
    public string Model { get; set; } = string.Empty;
}
