using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Validators.Todos;
using FluentValidation.TestHelper;

namespace Fistix.TaskManager.AiLayer.Tests;

public class ClassifyTodoTaskCommandValidatorTests
{
    private readonly ClassifyTodoTaskCommandValidator _validator = new();

    [Fact]
    public void Should_fail_when_todo_external_id_is_empty()
    {
        var command = new ClassifyTodoTaskCommand { TodoExternalId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TodoExternalId);
    }

    [Fact]
    public void Should_pass_when_todo_external_id_is_set()
    {
        var command = new ClassifyTodoTaskCommand { TodoExternalId = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
