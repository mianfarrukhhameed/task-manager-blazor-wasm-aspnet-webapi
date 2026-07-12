using Fistix.TaskManager.AiLayer.Shared;

namespace Fistix.TaskManager.AiLayer.Tests;

public class ClassificationGuardrailsTests
{
    [Theory]
    [InlineData("Fix production down issue", "HIGH")]
    [InlineData("Users cannot login to portal", "HIGH")]
    [InlineData("Update documentation", "MEDIUM")]
    public void Apply_enforces_expected_priority(string text, string expectedPriority)
    {
        var (priority, _, _) = ClassificationGuardrails.Apply(
            "MEDIUM",
            0.7f,
            null,
            text,
            string.Empty,
            dueDate: null);

        Assert.Equal(expectedPriority, priority);
    }

    [Fact]
    public void Apply_caps_chore_tasks_without_due_date_at_medium()
    {
        var (priority, confidence, _) = ClassificationGuardrails.Apply(
            "HIGH",
            0.9f,
            null,
            "Update documentation",
            "cleanup notes",
            dueDate: null);

        Assert.Equal("MEDIUM", priority);
        Assert.True(confidence <= 0.84f);
    }

    [Theory]
    [InlineData("HIGH", "High")]
    [InlineData("MEDIUM", "Medium")]
    [InlineData("LOW", "Low")]
    [InlineData("High", "High")]
    [InlineData("medium", "Medium")]
    [InlineData("low", "Low")]
    public void ToTaskPriority_maps_to_task_format(string input, string expected)
    {
        Assert.Equal(expected, ClassificationGuardrails.ToTaskPriority(input));
    }

    [Theory]
    [InlineData("High", "HIGH", false)]
    [InlineData("Medium", "MEDIUM", false)]
    [InlineData("Low", "LOW", false)]
    [InlineData("High", "MEDIUM", true)]
    [InlineData("high", "HIGH", false)]
    [InlineData("LOW", "High", true)]
    public void IsPriorityOverridden_compares_normalized_values(
        string taskPriority,
        string aiPriority,
        bool expected)
    {
        Assert.Equal(expected, ClassificationGuardrails.IsPriorityOverridden(taskPriority, aiPriority));
    }
}
