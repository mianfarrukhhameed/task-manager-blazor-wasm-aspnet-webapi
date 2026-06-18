using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Validators.Todos;
using FluentValidation.TestHelper;

namespace Fistix.TaskManager.AiLayer.Tests;

public class CreateTodoTaskCommandValidatorTests
{
    private readonly CreateTodoTaskCommandValidator _validator = new();

    [Fact]
    public void Should_fail_when_description_is_empty()
    {
        var command = new CreateTodoTaskCommand
        {
            Title = "Valid title",
            Description = "",
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_fail_when_title_exceeds_max_length()
    {
        var command = new CreateTodoTaskCommand
        {
            Title = new string('t', TodoFieldLimits.TitleMaxLength + 1),
            Description = "Valid description",
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }
}
