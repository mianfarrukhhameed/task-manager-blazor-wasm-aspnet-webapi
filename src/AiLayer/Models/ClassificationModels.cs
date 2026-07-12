using System;

namespace Fistix.TaskManager.AiLayer.Models;

public class ClassificationRequest
{
    public Guid TodoExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool Force { get; set; }
}

public class ClassificationResponse
{
    public Guid TodoExternalId { get; set; }
    public string SuggestedPriority { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string? Reason { get; set; }
    public string Model { get; set; } = string.Empty;
    public bool FromCache { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
