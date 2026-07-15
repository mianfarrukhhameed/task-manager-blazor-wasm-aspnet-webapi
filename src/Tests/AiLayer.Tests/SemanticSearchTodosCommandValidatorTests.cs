using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Validators.Todos;
using Xunit;

namespace Fistix.TaskManager.AiLayer.Tests;

public class SemanticSearchTodosCommandValidatorTests
{
    private readonly SemanticSearchTodosCommandValidator _validator = new();

    [Fact]
    public void RejectsEmptyQuery()
    {
        var result = _validator.Validate(new SemanticSearchTodosCommand { Query = "", Limit = 10 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AcceptsValidQuery()
    {
        var result = _validator.Validate(new SemanticSearchTodosCommand { Query = "payment flow", Limit = 10 });
        Assert.True(result.IsValid);
    }
}
