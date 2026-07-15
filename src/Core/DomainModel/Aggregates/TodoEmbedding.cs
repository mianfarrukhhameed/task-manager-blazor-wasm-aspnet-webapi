#nullable enable

using System;
using Pgvector;

namespace Fistix.TaskManager.Core.DomainModel.Aggregates;

/// <summary>
/// Stores vector embeddings for todo tasks (pgvector).
/// </summary>
public class TodoEmbedding
{
    public int Id { get; set; }
    public int TodoId { get; set; }
    public Vector Embedding { get; set; } = null!;
    public string EmbeddingModel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual TodoTask? TodoTask { get; set; }
}
