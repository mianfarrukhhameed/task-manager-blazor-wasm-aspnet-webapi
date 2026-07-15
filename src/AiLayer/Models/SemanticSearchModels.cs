#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;

namespace Fistix.TaskManager.AiLayer.Models;

public sealed class SemanticSearchPipelineRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
    public Guid? OwnerExternalId { get; set; }
}

public sealed class SemanticSearchPipelineResult
{
    public IReadOnlyList<VectorSearchHit> Hits { get; set; } = Array.Empty<VectorSearchHit>();
    public long ExecutionTimeMs { get; set; }
    public string Model { get; set; } = string.Empty;
}
