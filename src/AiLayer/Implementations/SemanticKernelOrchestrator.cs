using Anthropic.SDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Shared;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Orchestrates Semantic Kernel initialization based on provider configuration.
/// Supports OpenAI, Azure OpenAI, Google Gemini, Claude (via Anthropic.SDK), and Ollama.
/// </summary>
public class SemanticKernelOrchestrator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SemanticKernelOrchestrator> _logger;
    private Kernel? _kernel;

    public SemanticKernelOrchestrator(
        IConfiguration configuration,
        ILogger<SemanticKernelOrchestrator> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Kernel> CreateKernelAsync()
    {
        if (_kernel != null)
        {
            return _kernel;
        }

        var aiConfig = new AiConfiguration();
        _configuration.GetSection("Ai").Bind(aiConfig);

        _logger.LogInformation("Initializing Semantic Kernel with provider: {Provider}", aiConfig.Provider);

        var builder = Kernel.CreateBuilder();

        try
        {
            switch (aiConfig.Provider.ToLowerInvariant())
            {
                case "openai":
                    builder.AddOpenAIChatCompletion(
                        modelId: aiConfig.OpenAI.Model,
                        apiKey: ResolveApiKey(aiConfig.OpenAI.ApiKey));
                    _logger.LogInformation("Configured OpenAI provider with model: {Model}", aiConfig.OpenAI.Model);
                    break;

                case "azureopenai":
                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: aiConfig.AzureOpenAI.Model,
                        endpoint: aiConfig.AzureOpenAI.Endpoint,
                        apiKey: ResolveApiKey(aiConfig.AzureOpenAI.ApiKey));
                    _logger.LogInformation("Configured Azure OpenAI provider with model: {Model}", aiConfig.AzureOpenAI.Model);
                    break;

                case "google":
                    var googleApiVersion = ResolveGoogleApiVersion(aiConfig.GoogleAI.ApiVersion);
                    builder.AddGoogleAIGeminiChatCompletion(
                        modelId: aiConfig.GoogleAI.Model,
                        apiKey: ResolveApiKey(aiConfig.GoogleAI.ApiKey),
                        apiVersion: googleApiVersion,
                        serviceId: string.Empty,
                        httpClient: null);
                    _logger.LogInformation(
                        "Configured Google Gemini provider with model: {Model}, apiVersion: {ApiVersion}",
                        aiConfig.GoogleAI.Model,
                        googleApiVersion);
                    break;

                case "claude":
                    RegisterClaudeChatCompletion(builder, aiConfig);
                    _logger.LogInformation("Configured Claude provider via Anthropic.SDK with model: {Model}", aiConfig.Claude.Model);
                    break;

                case "ollama":
                    var ollamaEndpoint = aiConfig.Ollama.Endpoint?.Trim().TrimEnd('/') ?? "http://localhost:11434";
                    if (!ollamaEndpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                    {
                        ollamaEndpoint += "/v1";
                    }

                    builder.AddOpenAIChatCompletion(
                        modelId: aiConfig.Ollama.Model,
                        apiKey: "ollama",
                        endpoint: new Uri(ollamaEndpoint));
                    _logger.LogInformation("Configured Ollama provider with model: {Model} at endpoint: {Endpoint}", aiConfig.Ollama.Model, ollamaEndpoint);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported AI provider: {aiConfig.Provider}");
            }

            _kernel = builder.Build();
            _logger.LogInformation("Semantic Kernel initialized successfully");
            return _kernel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Semantic Kernel");
            throw;
        }
    }

    /// <summary>
    /// Builds a short-lived kernel for a specific Google model (used for fallback on transient errors).
    /// </summary>
    public Kernel CreateGoogleKernel(AiConfiguration aiConfig, string modelId)
    {
        var builder = Kernel.CreateBuilder();
        var googleApiVersion = ResolveGoogleApiVersion(aiConfig.GoogleAI.ApiVersion);

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: modelId,
            apiKey: ResolveApiKey(aiConfig.GoogleAI.ApiKey),
            apiVersion: googleApiVersion,
            serviceId: string.Empty,
            httpClient: null);

        return builder.Build();
    }

    private void RegisterClaudeChatCompletion(IKernelBuilder builder, AiConfiguration aiConfig)
    {
        var apiKey = ResolveApiKey(aiConfig.Claude.ApiKey);
        var anthropicClient = new AnthropicClient(apiKey);
        IChatClient chatClient = anthropicClient.Messages;

        builder.Services.AddSingleton(chatClient);
        builder.Services.AddSingleton<IChatCompletionService>(sp =>
            chatClient.AsChatCompletionService(sp));
    }

    private static GoogleAIVersion ResolveGoogleApiVersion(string? configuredValue)
    {
        return configuredValue?.Trim().ToLowerInvariant() switch
        {
            "v1" => GoogleAIVersion.V1,
            "v1_beta" or "v1beta" or "v1-beta" => GoogleAIVersion.V1_Beta,
            _ => GoogleAIVersion.V1_Beta,
        };
    }

    private string ResolveApiKey(string configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            throw new InvalidOperationException("API key is not configured");
        }

        if (configuredValue.StartsWith("${", StringComparison.Ordinal) && configuredValue.EndsWith("}", StringComparison.Ordinal))
        {
            var envVarName = configuredValue[2..^1];
            var envValue = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrWhiteSpace(envValue))
            {
                throw new InvalidOperationException($"Environment variable '{envVarName}' is not set. Required for AI configuration.");
            }

            return envValue;
        }

        return configuredValue;
    }
}

public class SemanticKernelLlmProvider : ILlmProviderService
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelLlmProvider> _logger;

    public SemanticKernelLlmProvider(
        Kernel kernel,
        ILogger<SemanticKernelLlmProvider> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(prompt));
        }

        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

        // ChatHistory(string) treats the text as a *system* message only.
        // Google Gemini (and some other providers) reject histories with only system messages.
        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await chatCompletionService.GetChatMessageContentAsync(
            history,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        return response.Content?.Trim() ?? string.Empty;
    }
}
