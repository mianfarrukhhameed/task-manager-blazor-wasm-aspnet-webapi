using System;

namespace Fistix.TaskManager.ViewModel.Dtos;

/// <summary>
/// DTO for task summarization response from AI API.
/// </summary>
public class TaskSummaryDto
{
    public Guid TodoExternalId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string Model { get; set; } = string.Empty;
    public bool FromCache { get; set; }
    public DateTime GeneratedAt { get; set; }
}
