using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Models;
using Fistix.TaskManager.AiLayer.Shared;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Pipeline for generating AI summaries of task descriptions.
/// Uses Semantic Kernel to call LLM and produce concise summaries.
/// Supports multiple providers: OpenAI, Azure OpenAI, Google AI, Ollama.
/// </summary>
public class SummarizationPipeline : IAiPipeline
{
    private readonly Kernel _kernel;
    private readonly GoogleAIService? _googleAIService;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<SummarizationPipeline> _logger;

    private const string SummarizationPrompt = @"
You are a task summarization expert. Your job is to create a concise, single-line summary of a task description.

RULES:
- Summary must be 1-2 sentences maximum (under 50 words)
- Capture the core essence and purpose
- Be specific and actionable
- Do not add speculation or assumptions not in the original description
- Use present tense

TASK TITLE: {title}
TASK DESCRIPTION: {description}

SUMMARY:";

    public SummarizationPipeline(
        Kernel kernel,
        ILogger<SummarizationPipeline> logger,
        GoogleAIService? googleAIService = null,
        AiConfiguration? aiConfig = null)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _googleAIService = googleAIService;
        _aiConfig = aiConfig ?? new AiConfiguration();
    }

    /// <summary>
    /// Execute the summarization pipeline.
    /// </summary>
    public async Task<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class
        where TResponse : class
    {
        if (request is not SummarizationRequest summarizationRequest)
        {
            throw new ArgumentException($"Request must be of type {nameof(SummarizationRequest)}", nameof(request));
        }

        try
        {
            _logger.LogInformation("Starting summarization for todo {TodoExternalId}", summarizationRequest.TodoExternalId);

            // Prepare the prompt
            var prompt = SummarizationPrompt
                .Replace("{title}", summarizationRequest.Title)
                .Replace("{description}", summarizationRequest.Description);

            string summary;
            string modelName;

            // Use Google AI service if provider is Google
            if (_aiConfig.Provider.Equals("google", StringComparison.OrdinalIgnoreCase) && _googleAIService != null)
            {
                summary = await _googleAIService.GenerateContentAsync(prompt);
                modelName = _aiConfig.GoogleAI.Model;
            }
            else
            {
                // Use Semantic Kernel for other providers
                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

                // Execute the prompt
                var response = await chatCompletionService.GetChatMessageContentAsync(
                    new ChatHistory(prompt),
                    kernel: _kernel);

                summary = response.Content?.Trim() ?? string.Empty;
                modelName = "semantic-kernel";
            }

            // Remove "SUMMARY:" prefix if present
            if (summary.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
            {
                summary = summary[8..].Trim();
            }

            _logger.LogInformation("Successfully summarized todo {TodoExternalId}. Summary length: {Length}", 
                summarizationRequest.TodoExternalId, summary.Length);

            var result = new SummarizationResponse
            {
                TodoExternalId = summarizationRequest.TodoExternalId,
                Summary = summary,
                TokensUsed = EstimateTokens(summary),
                Model = modelName,
                FromCache = false,
                GeneratedAt = DateTime.UtcNow
            };

            return result as TResponse ?? throw new InvalidOperationException("Failed to cast response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during summarization for todo {TodoExternalId}", summarizationRequest.TodoExternalId);
            throw;
        }
    }

    /// <summary>
    /// Rough estimation of tokens used (for display purposes).
    /// Actual token count would come from the LLM provider.
    /// </summary>
    private int EstimateTokens(string text)
    {
        // Rough estimate: ~4 characters per token
        return (text.Length + 3) / 4;
    }
}
