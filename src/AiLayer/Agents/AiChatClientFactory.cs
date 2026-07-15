#nullable enable

using System.ClientModel;
using Fistix.TaskManager.AiLayer.Shared;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Fistix.TaskManager.AiLayer.Agents;

/// <summary>
/// Builds an <see cref="IChatClient"/> for Microsoft Agent Framework from Ai configuration.
/// Google Gemini uses the OpenAI-compatible Generative Language endpoint so tool calling works.
/// </summary>
public sealed class AiChatClientFactory
{
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<AiChatClientFactory> _logger;

    public AiChatClientFactory(AiConfiguration aiConfig, ILogger<AiChatClientFactory> logger)
    {
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public IChatClient CreateChatClient()
    {
        var provider = (_aiConfig.Provider ?? "openai").Trim().ToLowerInvariant();
        return provider switch
        {
            "google" => CreateGoogleOpenAiCompatibleClient(),
            "azureopenai" => CreateAzureOpenAiClient(),
            "ollama" => CreateOllamaClient(),
            "claude" => throw new InvalidOperationException(
                "Microsoft Agent Framework sprint agent does not support Claude directly yet. Use Provider google, openai, azureopenai, or ollama."),
            _ => CreateOpenAiClient()
        };
    }

    private IChatClient CreateOpenAiClient()
    {
        var apiKey = ResolveApiKey(_aiConfig.OpenAI.ApiKey);
        var model = string.IsNullOrWhiteSpace(_aiConfig.OpenAI.Model) ? "gpt-4o-mini" : _aiConfig.OpenAI.Model;
        _logger.LogInformation("MAF chat client using OpenAI model {Model}", model);
        return new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
    }

    private IChatClient CreateAzureOpenAiClient()
    {
        var apiKey = ResolveApiKey(_aiConfig.AzureOpenAI.ApiKey);
        var endpoint = _aiConfig.AzureOpenAI.Endpoint?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Ai:AzureOpenAI:Endpoint is required for Azure OpenAI agents.");
        }

        var model = string.IsNullOrWhiteSpace(_aiConfig.AzureOpenAI.Model) ? "gpt-4o" : _aiConfig.AzureOpenAI.Model;
        var options = new OpenAIClientOptions { Endpoint = new Uri($"{endpoint}/openai/v1") };
        _logger.LogInformation("MAF chat client using Azure OpenAI deployment {Model}", model);
        return new OpenAIClient(new ApiKeyCredential(apiKey), options).GetChatClient(model).AsIChatClient();
    }

    private IChatClient CreateOllamaClient()
    {
        var endpoint = string.IsNullOrWhiteSpace(_aiConfig.Ollama.Endpoint)
            ? "http://localhost:11434"
            : _aiConfig.Ollama.Endpoint.TrimEnd('/');
        if (!endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint += "/v1";
        }

        var model = string.IsNullOrWhiteSpace(_aiConfig.Ollama.Model) ? "mistral:7b" : _aiConfig.Ollama.Model;
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        _logger.LogInformation("MAF chat client using Ollama model {Model} at {Endpoint}", model, endpoint);
        return new OpenAIClient(new ApiKeyCredential("ollama"), options).GetChatClient(model).AsIChatClient();
    }

    private IChatClient CreateGoogleOpenAiCompatibleClient()
    {
        var apiKey = ResolveApiKey(_aiConfig.GoogleAI.ApiKey);
        var model = string.IsNullOrWhiteSpace(_aiConfig.GoogleAI.Model) ? "gemini-2.5-flash" : _aiConfig.GoogleAI.Model;
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/")
        };
        _logger.LogInformation("MAF chat client using Google OpenAI-compatible model {Model}", model);
        return new OpenAIClient(new ApiKeyCredential(apiKey), options).GetChatClient(model).AsIChatClient();
    }

    private static string ResolveApiKey(string configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            throw new InvalidOperationException("API key is not configured for the active AI provider.");
        }

        if (configuredValue.StartsWith("${", StringComparison.Ordinal) && configuredValue.EndsWith('}'))
        {
            var envName = configuredValue[2..^1];
            return Environment.GetEnvironmentVariable(envName)
                ?? throw new InvalidOperationException($"Environment variable '{envName}' is not set.");
        }

        return configuredValue;
    }
}
