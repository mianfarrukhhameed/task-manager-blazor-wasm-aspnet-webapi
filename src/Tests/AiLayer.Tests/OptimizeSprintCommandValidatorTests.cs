using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Validators.Todos;
using Xunit;

namespace Fistix.TaskManager.AiLayer.Tests;

public class OptimizeSprintCommandValidatorTests
{
    private readonly OptimizeSprintCommandValidator _validator = new();

    [Fact]
    public void AcceptsValidCommand()
    {
        var result = _validator.Validate(new OptimizeSprintCommand
        {
            MaxTasks = 12,
            DurationDays = 14,
            Name = "Q2 Sprint"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMaxTasksOutOfRange()
    {
        var result = _validator.Validate(new OptimizeSprintCommand
        {
            MaxTasks = 0,
            DurationDays = 14
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RejectsDurationDaysOutOfRange()
    {
        var result = _validator.Validate(new OptimizeSprintCommand
        {
            MaxTasks = 12,
            DurationDays = 100
        });
        Assert.False(result.IsValid);
    }
}
