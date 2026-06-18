using Fistix.TaskManager.AiLayer.Shared;

namespace Fistix.TaskManager.AiLayer.Tests;

public class PromptInputSanitizerTests
{
    [Fact]
    public void Sanitize_EscapesSemanticKernelTemplateBraces()
    {
        var result = PromptInputSanitizer.Sanitize("Use {ignore} rules");

        Assert.Equal("Use {{ignore}} rules", result);
    }

    [Fact]
    public void SanitizeAndTruncate_TruncatesToMaxLength()
    {
        var input = new string('a', LlmInputLimits.DescriptionMaxLength + 100);

        var result = PromptInputSanitizer.SanitizeAndTruncate(input, LlmInputLimits.DescriptionMaxLength);

        Assert.Equal(LlmInputLimits.DescriptionMaxLength, result.Length);
    }

    [Fact]
    public void Sanitize_RemovesControlCharactersExceptNewlines()
    {
        var result = PromptInputSanitizer.Sanitize("line1\nline2\u0001");

        Assert.Equal("line1\nline2", result);
    }
}
