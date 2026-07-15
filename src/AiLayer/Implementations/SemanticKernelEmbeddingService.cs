#nullable enable

using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Generates embeddings via OpenAI-compatible APIs (OpenAI or Ollama).
/// </summary>
public sealed class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly AiConfiguration _aiConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SemanticKernelEmbeddingService> _logger;

    public SemanticKernelEmbeddingService(
        AiConfiguration aiConfig,
        IHttpClientFactory httpClientFactory,
        ILogger<SemanticKernelEmbeddingService> logger)
    {
        _aiConfig = aiConfig;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string ModelName => _aiConfig.Embedding.Model;
    public int Dimension => _aiConfig.Embedding.Dimension;

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        EmbeddingInputKind kind = EmbeddingInputKind.Passage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required to generate an embedding.", nameof(text));
        }

        // Optional: apply the same BGE-style query instruction for consistency across providers.
        var prepared = EmbeddingPooling.ApplyInputKind(
            text.Trim(),
            kind,
            _aiConfig.Embedding.Onnx?.QueryInstruction);

        var provider = _aiConfig.Embedding.Provider.Trim().ToLowerInvariant();
        return provider switch
        {
            "ollama" => await GenerateOllamaEmbeddingAsync(prepared, cancellationToken),
            _ => await GenerateOpenAiEmbeddingAsync(prepared, cancellationToken)
        };
    }

    private async Task<float[]> GenerateOpenAiEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var apiKey = ResolveApiKey(_aiConfig.Embedding.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(_aiConfig.OpenAI.ApiKey))
        {
            apiKey = ResolveApiKey(_aiConfig.OpenAI.ApiKey);
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Embedding API key is not configured (Ai:Embedding:ApiKey or Ai:OpenAI:ApiKey).");
        }

        var endpoint = string.IsNullOrWhiteSpace(_aiConfig.Embedding.Endpoint)
            ? "https://api.openai.com/v1"
            : _aiConfig.Embedding.Endpoint.TrimEnd('/');

        var client = _httpClientFactory.CreateClient(nameof(SemanticKernelEmbeddingService));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = ModelName,
            ["input"] = text,
            ["dimensions"] = Dimension
        };
        request.Content = JsonContent.Create(payload);

        _logger.LogInformation("Generating embedding with model {Model} (dim={Dimension})", ModelName, Dimension);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Embedding provider returned {(int)response.StatusCode}: {Truncate(body)}");
        }

        using var doc = JsonDocument.Parse(body);
        var embedding = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding");

        var values = new float[embedding.GetArrayLength()];
        var i = 0;
        foreach (var item in embedding.EnumerateArray())
        {
            values[i++] = item.GetSingle();
        }

        return values;
    }

    private async Task<float[]> GenerateOllamaEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(_aiConfig.Embedding.Endpoint)
            ? _aiConfig.Ollama.Endpoint.TrimEnd('/')
            : _aiConfig.Embedding.Endpoint.TrimEnd('/');

        var client = _httpClientFactory.CreateClient(nameof(SemanticKernelEmbeddingService));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/api/embeddings");
        request.Content = JsonContent.Create(new
        {
            model = ModelName,
            prompt = text
        });

        _logger.LogInformation("Generating Ollama embedding with model {Model}", ModelName);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Ollama embedding returned {(int)response.StatusCode}: {Truncate(body)}");
        }

        using var doc = JsonDocument.Parse(body);
        var embedding = doc.RootElement.GetProperty("embedding");
        var values = new float[embedding.GetArrayLength()];
        var i = 0;
        foreach (var item in embedding.EnumerateArray())
        {
            values[i++] = item.GetSingle();
        }

        if (values.Length != Dimension)
        {
            _logger.LogWarning(
                "Ollama embedding dimension {Actual} differs from configured {Expected}",
                values.Length,
                Dimension);
        }

        return values;
    }

    private static string ResolveApiKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith('}'))
        {
            var envName = value[2..^1];
            return Environment.GetEnvironmentVariable(envName) ?? string.Empty;
        }

        return value;
    }

    private static string Truncate(string value) =>
        value.Length <= 300 ? value : value[..300] + "...";
}
