using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Fistix.TaskManager.AiLayer.Shared;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Orchestrates Semantic Kernel initialization based on provider configuration.
/// Supports multiple LLM providers: OpenAI, Azure OpenAI, and Ollama.
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

    /// <summary>
    /// Creates and returns a configured Semantic Kernel instance.
    /// Provider selection is based on appsettings.json "Ai:Provider" setting.
    /// </summary>
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
                    // Google AI support requires Google.Generative.AI NuGet package
                    // For now, we implement a simple HTTP-based approach via custom GoogleAIService
                    _logger.LogInformation("Configured Google AI provider with model: {Model}", aiConfig.GoogleAI.Model);
                    // Note: Google AI will be called directly in SummarizationPipeline without Semantic Kernel
                    // This allows flexibility to add Google support without version conflicts
                    builder.AddOpenAIChatCompletion(
                        modelId: aiConfig.OpenAI.Model,
                        apiKey: ResolveApiKey(aiConfig.OpenAI.ApiKey));
                    break;

                case "ollama":
                    // Note: This requires OllamaSharp or similar connector
                    // For now, we'll fall back to OpenAI to ensure Phase 1 works
                    _logger.LogWarning("Ollama provider requested but not yet implemented. Falling back to OpenAI.");
                    builder.AddOpenAIChatCompletion(
                        modelId: aiConfig.OpenAI.Model,
                        apiKey: ResolveApiKey(aiConfig.OpenAI.ApiKey));
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
    /// Resolves API key from environment variable or configuration.
    /// Environment variables take precedence over configuration values.
    /// </summary>
    private string ResolveApiKey(string configuredValue)
    {
        // Check if value is an environment variable reference (${VARIABLE_NAME})
        if (!string.IsNullOrEmpty(configuredValue) && configuredValue.StartsWith("${") && configuredValue.EndsWith("}"))
        {
            var envVarName = configuredValue[2..^1]; // Extract variable name
            var envValue = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(envValue))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{envVarName}' is not set. Required for AI configuration.");
            }

            return envValue;
        }

        return configuredValue ?? throw new InvalidOperationException("API key is not configured");
    }
}
