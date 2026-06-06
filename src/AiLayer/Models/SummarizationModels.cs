using System;

namespace Fistix.TaskManager.AiLayer.Models;

public class SummarizationRequest
{
    public Guid TodoExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Force { get; set; } = false; // Force regeneration even if cached
}

public class SummarizationResponse
{
    public Guid TodoExternalId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string Model { get; set; } = string.Empty;
    public bool FromCache { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
