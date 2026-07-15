#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Fistix.TaskManager.AiLayer.Implementations;

public sealed class SemanticSearchPipeline
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<SemanticSearchPipeline> _logger;

    public SemanticSearchPipeline(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<SemanticSearchPipeline> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<SemanticSearchPipelineResult> ExecuteAsync(
        SemanticSearchPipelineRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
        var hits = await _vectorStore.SearchAsync(
            embedding,
            _embeddingService.ModelName,
            request.OwnerExternalId,
            request.Limit,
            cancellationToken);
        sw.Stop();

        _logger.LogInformation(
            "Semantic search returned {Count} hits in {ElapsedMs}ms for model {Model}",
            hits.Count,
            sw.ElapsedMilliseconds,
            _embeddingService.ModelName);

        return new SemanticSearchPipelineResult
        {
            Hits = hits,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Model = _embeddingService.ModelName
        };
    }
}
