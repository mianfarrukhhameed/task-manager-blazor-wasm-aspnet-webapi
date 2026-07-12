using Fistix.TaskManager.ViewModel.Commands.Todos;
using FluentValidation;

namespace Fistix.TaskManager.ViewModel.Validators.Todos;

public class ClassifyTodoTaskCommandValidator : AbstractValidator<ClassifyTodoTaskCommand>
{
    public ClassifyTodoTaskCommandValidator()
    {
        RuleFor(x => x.TodoExternalId).NotEmpty();
    }
}
