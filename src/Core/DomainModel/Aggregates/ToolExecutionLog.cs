#nullable enable

using System;

namespace Fistix.TaskManager.Core.DomainModel.Aggregates;

public class ToolExecutionLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public string? Result { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
