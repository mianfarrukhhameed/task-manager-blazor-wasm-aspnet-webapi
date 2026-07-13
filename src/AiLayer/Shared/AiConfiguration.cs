namespace Fistix.TaskManager.AiLayer.Shared;

/// <summary>
/// Configuration settings for AI features and LLM providers.
/// </summary>
public class AiConfiguration
{
    public string Provider { get; set; } = "OpenAI";

    public OpenAiSettings OpenAI { get; set; } = new();
    public AzureOpenAISettings AzureOpenAI { get; set; } = new();
    public OllamaSettings Ollama { get; set; } = new();
    public GoogleAISettings GoogleAI { get; set; } = new();
    public ClaudeSettings Claude { get; set; } = new();
    public EmbeddingSettings Embedding { get; set; } = new();
    public AiFeaturesConfiguration Features { get; set; } = new();
}

public class EmbeddingSettings
{
    /// <summary>OpenAI or Ollama (OpenAI-compatible embeddings API).</summary>
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = "text-embedding-3-small";
    public int Dimension { get; set; } = 384;
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
}

public class OpenAiSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
}

public class AzureOpenAISettings
{
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string Model { get; set; } = "gpt-4";
}

public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "mistral:7b";
}

public class GoogleAISettings
{
    // API key (supports environment variable reference pattern: ${ENV_VAR})
    public string ApiKey { get; set; } = "";

    // Optional path to a service account JSON file when using Vertex/GenAI with service-account credentials
    public string ServiceAccountJsonPath { get; set; } = "";

    public string Model { get; set; } = "";

    // V1 or V1_Beta — most Gemini chat models require V1_Beta
    public string ApiVersion { get; set; } = "";

    // Used when the primary model returns transient errors (503/429)
    public string[] FallbackModels { get; set; } = [];
}

public class ClaudeSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-3-5-sonnet-latest";
    public string Endpoint { get; set; } = "https://api.anthropic.com";
}

public class AiFeaturesConfiguration
{
    public bool EnableSummarization { get; set; } = true;
    public AiRateLimitConfiguration SummarizeRateLimit { get; set; } = new();
    public bool EnableClassification { get; set; } = false;
    public AiRateLimitConfiguration ClassifyRateLimit { get; set; } = new();
    public ClassificationConfiguration Classification { get; set; } = new();
    public bool EnableEmbeddings { get; set; } = false;
    public bool EnableSemanticSearch { get; set; } = false;
    public AiRateLimitConfiguration SemanticSearchRateLimit { get; set; } = new();
    public bool EnableRag { get; set; } = false;
    public AiRateLimitConfiguration RagRateLimit { get; set; } = new();
    public bool EnableFunctionCalling { get; set; } = false;
    public AiRateLimitConfiguration FunctionCallingRateLimit { get; set; } = new();
    public bool EnableAgents { get; set; } = false;
    public AiRateLimitConfiguration AgentsRateLimit { get; set; } = new();
    public bool EnableMcp { get; set; } = false;
}
