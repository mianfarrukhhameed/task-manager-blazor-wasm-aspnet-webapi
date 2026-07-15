#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Fistix.TaskManager.AiLayer.Implementations;

public sealed class RAGPipeline
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILlmProviderService _llm;
    private readonly ILogger<RAGPipeline> _logger;

    public RAGPipeline(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILlmProviderService llm,
        ILogger<RAGPipeline> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llm = llm;
        _logger = logger;
    }

    public async Task<RagPipelineResult> ExecuteAsync(
        RagPipelineRequest request,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);
        var hits = await _vectorStore.SearchAsync(
            queryEmbedding,
            _embeddingService.ModelName,
            request.OwnerExternalId,
            request.TopK,
            cancellationToken);

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine($"Context focus: {request.Context}");
        foreach (var source in request.SourceTodos)
        {
            contextBuilder.AppendLine($"- [{source.ExternalId}] {source.Title} | priority={source.Priority} status={source.Status} due={source.DueDate:u}");
            if (!string.IsNullOrWhiteSpace(source.Description))
            {
                contextBuilder.AppendLine($"  {source.Description.Trim()}");
            }
        }

        var prompt = $"""
            You are a task-management assistant. Answer the user's question using ONLY the provided task context.
            If the context is insufficient, say what is missing. Be concise and cite task titles.

            Task context:
            {contextBuilder}

            Question: {request.Question}
            """;

        _logger.LogInformation("Running RAG for context {Context} with {Count} sources", request.Context, request.SourceTodos.Count);
        var answer = await _llm.GetCompletionAsync(prompt, cancellationToken);

        return new RagPipelineResult
        {
            Answer = answer.Trim(),
            SourceTodoIds = hits.Select(h => h.TodoExternalId).ToList(),
            Model = _embeddingService.ModelName
        };
    }
}
