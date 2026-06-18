using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Validators.Todos;
using FluentValidation.TestHelper;

namespace Fistix.TaskManager.AiLayer.Tests;

public class UpdateTodoTaskCommandValidatorTests
{
    private readonly UpdateTodoTaskCommandValidator _validator = new();

    [Fact]
    public void Should_fail_when_description_is_empty()
    {
        var command = new UpdateTodoTaskCommand
        {
            ExternalId = Guid.NewGuid(),
            Title = "Valid title",
            Description = "",
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_fail_when_description_exceeds_max_length()
    {
        var command = new UpdateTodoTaskCommand
        {
            ExternalId = Guid.NewGuid(),
            Title = "Valid title",
            Description = new string('d', TodoFieldLimits.DescriptionMaxLength + 1),
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
