using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Models;
using Fistix.TaskManager.AiLayer.Shared;
using System.Net;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Pipeline for generating AI summaries of task descriptions.
/// Uses Semantic Kernel prompt templating and chat completion.
/// </summary>
public class SummarizationPipeline : IAiPipeline
{
    private readonly Kernel _kernel;
    private readonly SemanticKernelOrchestrator _orchestrator;
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
- Only summarize the task data provided below

TASK TITLE: {{$title}}
TASK DESCRIPTION: {{$description}}

SUMMARY:";

    public SummarizationPipeline(
        Kernel kernel,
        SemanticKernelOrchestrator orchestrator,
        ILogger<SummarizationPipeline> logger,
        AiConfiguration? aiConfig = null)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiConfig = aiConfig ?? new AiConfiguration();
    }

    public async Task<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class
        where TResponse : class
    {
        if (request is not SummarizationRequest summarizationRequest)
        {
            throw new ArgumentException($"Request must be of type {nameof(SummarizationRequest)}", nameof(request));
        }

        _logger.LogInformation("Starting summarization for todo {TodoExternalId}", summarizationRequest.TodoExternalId);

        var arguments = new KernelArguments
        {
            ["title"] = PromptInputSanitizer.Sanitize(summarizationRequest.Title),
            ["description"] = PromptInputSanitizer.Sanitize(summarizationRequest.Description)
        };

        Exception? lastException = null;

        foreach (var (kernel, modelLabel) in GetKernelsToTry())
        {
            try
            {
                var summary = await InvokeLlmAsync(kernel, arguments, modelLabel);

                if (summary.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                {
                    summary = summary[8..].Trim();
                }

                _logger.LogInformation(
                    "Successfully summarized todo {TodoExternalId} using {Model}. Summary length: {Length}",
                    summarizationRequest.TodoExternalId,
                    modelLabel,
                    summary.Length);

                var response = new SummarizationResponse
                {
                    TodoExternalId = summarizationRequest.TodoExternalId,
                    Summary = summary,
                    TokensUsed = EstimateTokens(summary),
                    Model = modelLabel,
                    FromCache = false,
                    GeneratedAt = DateTime.UtcNow
                };

                return response as TResponse ?? throw new InvalidOperationException("Failed to cast response");
            }
            catch (Exception ex) when (IsTransientLlmError(ex))
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Transient LLM error for todo {TodoExternalId} using model {Model}; trying next fallback if available",
                    summarizationRequest.TodoExternalId,
                    modelLabel);
            }
        }

        _logger.LogError(lastException,
            "Error during summarization for todo {TodoExternalId} after all fallback models",
            summarizationRequest.TodoExternalId);

        throw lastException ?? new InvalidOperationException("Summarization failed with no captured exception.");
    }

    private IEnumerable<(Kernel Kernel, string ModelLabel)> GetKernelsToTry()
    {
        if (!_aiConfig.Provider.Equals("google", StringComparison.OrdinalIgnoreCase))
        {
            yield return (_kernel, GetModelName());
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var models = new[] { _aiConfig.GoogleAI.Model }
            .Concat(_aiConfig.GoogleAI.FallbackModels ?? []);

        var isPrimary = true;
        foreach (var model in models)
        {
            if (string.IsNullOrWhiteSpace(model) || !seen.Add(model))
            {
                continue;
            }

            yield return (isPrimary ? _kernel : _orchestrator.CreateGoogleKernel(_aiConfig, model), model);
            isPrimary = false;
        }
    }

    private async Task<string> InvokeLlmAsync(Kernel kernel, KernelArguments arguments, string modelLabel)
    {
        var function = kernel.CreateFunctionFromPrompt(SummarizationPrompt);
        var renderedPrompt = RenderPrompt(arguments);

        _logger.LogInformation(
            "LLM request -> Provider: {Provider}, Model: {Model}, Title: {Title}, Description: {Description}, Prompt: {Prompt}",
            _aiConfig.Provider,
            modelLabel,
            arguments["title"],
            arguments["description"],
            renderedPrompt);

        try
        {
            var result = await kernel.InvokeAsync(function, arguments);
            var rawResponse = result.GetValue<string>()?.Trim() ?? string.Empty;

            _logger.LogInformation(
                "LLM response <- Provider: {Provider}, Model: {Model}, RawResponse: {RawResponse}",
                _aiConfig.Provider,
                modelLabel,
                rawResponse);

            return rawResponse;
        }
        catch (HttpOperationException httpEx)
        {
            _logger.LogWarning(
                "LLM error <- Provider: {Provider}, Model: {Model}, Status: {StatusCode}, Response: {ErrorResponse}",
                _aiConfig.Provider,
                modelLabel,
                httpEx.StatusCode,
                httpEx.ResponseContent);
            throw;
        }
    }

    private static string RenderPrompt(KernelArguments arguments)
    {
        return SummarizationPrompt
            .Replace("{{$title}}", arguments["title"]?.ToString() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{{$description}}", arguments["description"]?.ToString() ?? string.Empty, StringComparison.Ordinal);
    }

    private static bool IsTransientLlmError(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpOperationException { StatusCode: HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout })
            {
                return true;
            }
        }

        return false;
    }

    private string GetModelName()
    {
        return _aiConfig.Provider.ToLowerInvariant() switch
        {
            "google" => _aiConfig.GoogleAI.Model,
            "azureopenai" => _aiConfig.AzureOpenAI.Model,
            "ollama" => _aiConfig.Ollama.Model,
            "claude" => _aiConfig.Claude.Model,
            _ => _aiConfig.OpenAI.Model,
        };
    }

    private static int EstimateTokens(string text)
    {
        return (text.Length + 3) / 4;
    }
}
