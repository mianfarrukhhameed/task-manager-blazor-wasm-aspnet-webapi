using Fistix.TaskManager.AiLayer.Shared;
using Xunit;

namespace Fistix.TaskManager.ServiceLayer.Tests;

public class EmbeddingProcessorTests
{
    [Fact]
    public void BuildEmbeddingText_CombinesTitleAndDescription()
    {
        var text = Fistix.TaskManager.ServiceLayer.Todos.EmbeddingProcessor.BuildEmbeddingText(
            "  Title  ",
            "  Description  ");

        Assert.Equal("Title\nDescription", text);
    }

    [Fact]
    public void BuildEmbeddingText_UsesTitleOnlyWhenDescriptionMissing()
    {
        var text = Fistix.TaskManager.ServiceLayer.Todos.EmbeddingProcessor.BuildEmbeddingText("Title", null);
        Assert.Equal("Title", text);
    }
}
