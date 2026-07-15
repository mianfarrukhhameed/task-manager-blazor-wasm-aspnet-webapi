#nullable enable

namespace Fistix.TaskManager.AiLayer.Models;

public sealed class RagPipelineRequest
{
    public string Question { get; set; } = string.Empty;
    public string Context { get; set; } = "workload";
    public IReadOnlyList<RagSourceTodo> SourceTodos { get; set; } = Array.Empty<RagSourceTodo>();
}

public sealed class RagSourceTodo
{
    public Guid ExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public DateTime DueDate { get; set; }
}

public sealed class RagPipelineResult
{
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<Guid> SourceTodoIds { get; set; } = Array.Empty<Guid>();
    public string Model { get; set; } = string.Empty;
}
