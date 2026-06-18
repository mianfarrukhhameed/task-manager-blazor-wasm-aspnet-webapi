using Fistix.TaskManager.AiLayer.Shared;

namespace Fistix.TaskManager.AiLayer.Tests;

public class LlmOutputValidatorTests
{
    [Fact]
    public void ValidateSummary_ThrowsWhenEmpty()
    {
        Assert.Throws<InvalidOperationException>(() => LlmOutputValidator.ValidateSummary("   "));
    }

    [Fact]
    public void ValidateSummary_TruncatesWhenOverMaxLength()
    {
        var longSummary = new string('x', LlmInputLimits.SummaryMaxLength + 50);

        var result = LlmOutputValidator.ValidateSummary(longSummary);

        Assert.Equal(LlmInputLimits.SummaryMaxLength, result.Length);
    }

    [Fact]
    public void ValidateSummary_ReturnsTrimmedSummary()
    {
        var result = LlmOutputValidator.ValidateSummary("  Hello world  ");

        Assert.Equal("Hello world", result);
    }
}
