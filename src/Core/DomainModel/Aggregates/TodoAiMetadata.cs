#nullable enable

using System;

namespace Fistix.TaskManager.Core.DomainModel.Aggregates;

/// <summary>
/// Stores AI-generated metadata for tasks (summaries, classifications, etc).
/// One-to-one relationship with TodoTask.
/// </summary>
public class TodoAiMetadata
{
    public int Id { get; set; }
    public int TodoId { get; set; }
    public string? AiSummary { get; set; }
    public string? AiPriority { get; set; } // HIGH, MEDIUM, LOW
    public string? AiCategory { get; set; } // Frontend, Backend, etc
    public string? AiType { get; set; } // Task, Reminder, etc
    public float? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual TodoTask? TodoTask { get; set; }
}
