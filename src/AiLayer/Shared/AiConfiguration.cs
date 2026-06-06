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
    public AiFeaturesConfiguration Features { get; set; } = new();
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
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-1.5-flash";
}

public class AiFeaturesConfiguration
{
    public bool EnableSummarization { get; set; } = true;
    public bool EnableClassification { get; set; } = false;
    public bool EnableSemanticSearch { get; set; } = false;
    public bool EnableRag { get; set; } = false;
    public bool EnableFunctionCalling { get; set; } = false;
    public bool EnableAgents { get; set; } = false;
    public bool EnableMcp { get; set; } = false;
}
