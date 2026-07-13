using MediatR;
using System.Collections.Generic;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class SemanticSearchTodosCommand : IRequest<SemanticSearchTodosCommandResult>
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
}

public class SemanticSearchTodosCommandResult
{
    public SemanticSearchResponseDto Payload { get; set; } = new();
}

public class SemanticSearchResponseDto
{
    public List<SemanticSearchHitDto> Results { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
    public string Model { get; set; } = string.Empty;
}

public class SemanticSearchHitDto
{
    public System.Guid TodoExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public double Similarity { get; set; }
}
