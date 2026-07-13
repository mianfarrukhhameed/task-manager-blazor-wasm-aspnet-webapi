using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Validators.Todos;
using Xunit;

namespace Fistix.TaskManager.AiLayer.Tests;

public class AiQueryCommandValidatorTests
{
    private readonly AiQueryCommandValidator _validator = new();

    [Theory]
    [InlineData("week")]
    [InlineData("project")]
    [InlineData("workload")]
    public void AcceptsAllowedContexts(string context)
    {
        var result = _validator.Validate(new AiQueryCommand { Question = "What am I working on?", Context = context });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsInvalidContext()
    {
        var result = _validator.Validate(new AiQueryCommand { Question = "What am I working on?", Context = "galaxy" });
        Assert.False(result.IsValid);
    }
}
