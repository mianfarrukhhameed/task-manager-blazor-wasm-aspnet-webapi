#nullable enable

namespace Fistix.TaskManager.AiLayer.Abstractions;

public interface IEmbeddingService
{
    string ModelName { get; }
    int Dimension { get; }
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public interface IVectorStore
{
    Task UpsertTodoEmbeddingAsync(int todoId, float[] embedding, string model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        float[] queryEmbedding,
        string embeddingModel,
        Guid? ownerExternalId,
        int limit,
        CancellationToken cancellationToken = default);
}

public sealed record VectorSearchHit(Guid TodoExternalId, int TodoId, double Similarity);
