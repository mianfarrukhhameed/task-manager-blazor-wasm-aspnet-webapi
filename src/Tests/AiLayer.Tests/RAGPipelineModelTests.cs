#nullable enable

using Fistix.TaskManager.AiLayer.Implementations;
using Fistix.TaskManager.AiLayer.Shared;
using Xunit;

namespace Fistix.TaskManager.AiLayer.Tests;

public class RAGPipelineModelTests
{
    [Theory]
    [InlineData("google", "gemini-3.5-flash")]
    [InlineData("openai", "gpt-4o-mini")]
    [InlineData("claude", "claude-3-5-sonnet-latest")]
    [InlineData("ollama", "mistral:7b")]
    public void ResolveChatModel_UsesProviderChatModel(string provider, string expectedModel)
    {
        var config = new AiConfiguration
        {
            Provider = provider,
            GoogleAI = new GoogleAISettings { Model = "gemini-3.5-flash" },
            OpenAI = new OpenAiSettings { Model = "gpt-4o-mini" },
            Claude = new ClaudeSettings { Model = "claude-3-5-sonnet-latest" },
            Ollama = new OllamaSettings { Model = "mistral:7b" },
            Embedding = new EmbeddingSettings { Model = "bge-small-en-v1.5" }
        };

        Assert.Equal(expectedModel, RAGPipeline.ResolveChatModel(config));
    }

    [Fact]
    public void ResolveChatModel_DoesNotReturnEmbeddingModel()
    {
        var config = new AiConfiguration
        {
            Provider = "google",
            GoogleAI = new GoogleAISettings { Model = "gemini-2.5-flash" },
            Embedding = new EmbeddingSettings { Model = "bge-small-en-v1.5" }
        };

        Assert.Equal("gemini-2.5-flash", RAGPipeline.ResolveChatModel(config));
        Assert.DoesNotContain("bge", RAGPipeline.ResolveChatModel(config), StringComparison.OrdinalIgnoreCase);
    }
}
