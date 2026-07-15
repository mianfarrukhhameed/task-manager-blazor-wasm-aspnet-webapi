namespace Fistix.TaskManager.AiLayer.Shared;

public class ClassificationConfiguration
{
    public int RequestTimeoutMs { get; set; } = 15000;
    public int MaxRetries { get; set; } = 1;
    public int RetryDelayMs { get; set; } = 500;

    /// <summary>
    /// Max concurrent background classification jobs. Keeps one slow LLM call from blocking the queue.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 3;
}
