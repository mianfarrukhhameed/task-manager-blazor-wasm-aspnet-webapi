using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Models;
using Fistix.TaskManager.AiLayer.Shared;
using System.Net;
using System.Text.Json;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Pipeline for classifying task priority from title and description.
/// </summary>
public class ClassificationPipeline : IAiPipeline
{
    private readonly Kernel _kernel;
    private readonly SemanticKernelOrchestrator _orchestrator;
    private readonly AiConfiguration _aiConfig;
    private readonly ClassificationConfiguration _classificationConfig;
    private readonly ILogger<ClassificationPipeline> _logger;

    private const string ClassificationPrompt = @"
You are a task priority classifier. Analyze the task and return ONLY valid JSON with no markdown.

RULES:
- priority must be one of: HIGH, MEDIUM, LOW
- confidence must be a number between 0 and 1
- reason must be one short sentence
- HIGH: production impact, blockers, security, payment/auth failures, urgent deadlines
- MEDIUM: normal work items with moderate impact
- LOW: nice-to-have, chores, documentation, low urgency
- Only use task data between the delimiters below

<task_title>
{{$title}}
</task_title>

<task_description>
{{$description}}
</task_description>

<due_date>
{{$dueDate}}
</due_date>

Return JSON exactly in this shape:
{""priority"":""HIGH|MEDIUM|LOW"",""confidence"":0.0,""reason"":""short reason""}";

    public ClassificationPipeline(
        Kernel kernel,
        SemanticKernelOrchestrator orchestrator,
        ILogger<ClassificationPipeline> logger,
        AiConfiguration? aiConfig = null,
        ClassificationConfiguration? classificationConfig = null)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiConfig = aiConfig ?? new AiConfiguration();
        _classificationConfig = classificationConfig ?? _aiConfig.Features.Classification ?? new ClassificationConfiguration();
    }

    public async Task<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class
        where TResponse : class
    {
        if (request is not ClassificationRequest classificationRequest)
        {
            throw new ArgumentException($"Request must be of type {nameof(ClassificationRequest)}", nameof(request));
        }

        _logger.LogInformation("Starting classification for todo {TodoExternalId}", classificationRequest.TodoExternalId);

        var arguments = new KernelArguments
        {
            ["title"] = PromptInputSanitizer.SanitizeAndTruncate(
                classificationRequest.Title, LlmInputLimits.TitleMaxLength),
            ["description"] = PromptInputSanitizer.SanitizeAndTruncate(
                classificationRequest.Description, LlmInputLimits.DescriptionMaxLength),
            ["dueDate"] = classificationRequest.DueDate?.ToString("yyyy-MM-dd") ?? "not set"
        };

        Exception? lastException = null;
        var attempts = Math.Max(1, _classificationConfig.MaxRetries + 1);

        foreach (var (kernel, modelLabel) in GetKernelsToTry())
        {
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(_classificationConfig.RetryDelayMs);
                }

                try
                {
                    using var timeoutCts = new CancellationTokenSource(_classificationConfig.RequestTimeoutMs);
                    var rawResponse = await InvokeLlmAsync(kernel, arguments, modelLabel, timeoutCts.Token);
                    var parsed = ParseResponse(rawResponse);

                    var (priority, confidence, reason) = ClassificationGuardrails.Apply(
                        parsed.Priority,
                        parsed.Confidence,
                        parsed.Reason,
                        classificationRequest.Title,
                        classificationRequest.Description,
                        classificationRequest.DueDate);

                    _logger.LogInformation(
                        "Successfully classified todo {TodoExternalId} using {Model}. Priority: {Priority}, Confidence: {Confidence}",
                        classificationRequest.TodoExternalId,
                        modelLabel,
                        priority,
                        confidence);

                    var response = new ClassificationResponse
                    {
                        TodoExternalId = classificationRequest.TodoExternalId,
                        SuggestedPriority = priority,
                        Confidence = confidence,
                        Reason = reason,
                        Model = modelLabel,
                        FromCache = false,
                        GeneratedAt = DateTime.UtcNow
                    };

                    return response as TResponse ?? throw new InvalidOperationException("Failed to cast response");
                }
                catch (Exception ex) when (IsTransientLlmError(ex) && attempt < attempts - 1)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "Transient LLM error for todo {TodoExternalId} using model {Model}, attempt {Attempt}",
                        classificationRequest.TodoExternalId,
                        modelLabel,
                        attempt + 1);
                }
                catch (Exception ex) when (IsTransientLlmError(ex))
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "Transient LLM error for todo {TodoExternalId} using model {Model}; trying next fallback if available",
                        classificationRequest.TodoExternalId,
                        modelLabel);
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "Non-retryable classification error for todo {TodoExternalId} using model {Model}",
                        classificationRequest.TodoExternalId,
                        modelLabel);
                    break;
                }
            }
        }

        _logger.LogError(lastException,
            "Error during classification for todo {TodoExternalId} after all attempts",
            classificationRequest.TodoExternalId);

        throw lastException ?? new InvalidOperationException("Classification failed with no captured exception.");
    }

    private static (string Priority, float Confidence, string? Reason) ParseResponse(string rawResponse)
    {
        var json = ExtractJson(rawResponse);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var priority = root.TryGetProperty("priority", out var priorityElement)
            ? priorityElement.GetString() ?? "MEDIUM"
            : "MEDIUM";

        var confidence = root.TryGetProperty("confidence", out var confidenceElement) && confidenceElement.TryGetSingle(out var value)
            ? value
            : 0.5f;

        var reason = root.TryGetProperty("reason", out var reasonElement)
            ? reasonElement.GetString()
            : null;

        return (ClassificationGuardrails.NormalizePriority(priority), confidence, reason);
    }

    private static string ExtractJson(string rawResponse)
    {
        var trimmed = rawResponse.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                return trimmed[start..(end + 1)];
            }
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return trimmed[firstBrace..(lastBrace + 1)];
        }

        throw new InvalidOperationException("AI returned an invalid classification response.");
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

    private async Task<string> InvokeLlmAsync(
        Kernel kernel,
        KernelArguments arguments,
        string modelLabel,
        CancellationToken cancellationToken)
    {
        var function = kernel.CreateFunctionFromPrompt(ClassificationPrompt);

        _logger.LogInformation(
            "LLM classify request -> Provider: {Provider}, Model: {Model}, TitleLength: {TitleLength}, DescriptionLength: {DescriptionLength}",
            _aiConfig.Provider,
            modelLabel,
            arguments["title"]?.ToString()?.Length ?? 0,
            arguments["description"]?.ToString()?.Length ?? 0);

        try
        {
            var result = await kernel.InvokeAsync(function, arguments, cancellationToken);
            var rawResponse = result.GetValue<string>()?.Trim() ?? string.Empty;

            _logger.LogInformation(
                "LLM classify response <- Provider: {Provider}, Model: {Model}, ResponseLength: {ResponseLength}",
                _aiConfig.Provider,
                modelLabel,
                rawResponse.Length);

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                throw new InvalidOperationException("AI returned an empty classification response.");
            }

            return rawResponse;
        }
        catch (HttpOperationException httpEx)
        {
            _logger.LogWarning(
                "LLM classify error <- Provider: {Provider}, Model: {Model}, Status: {StatusCode}",
                _aiConfig.Provider,
                modelLabel,
                httpEx.StatusCode);
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Classification timed out after {_classificationConfig.RequestTimeoutMs}ms.");
        }
    }

    private static bool IsTransientLlmError(Exception ex)
    {
        if (ex is TimeoutException)
        {
            return true;
        }

        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpOperationException
                {
                    StatusCode: HttpStatusCode.ServiceUnavailable
                        or HttpStatusCode.TooManyRequests
                        or HttpStatusCode.BadGateway
                        or HttpStatusCode.GatewayTimeout
                })
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
}
