using Fistix.TaskManager.ViewModel.Commands.Todos;
using FluentValidation;

namespace Fistix.TaskManager.ViewModel.Validators.Todos;

public class SummarizeTodoTaskCommandValidator : AbstractValidator<SummarizeTodoTaskCommand>
{
    public SummarizeTodoTaskCommandValidator()
    {
        RuleFor(x => x.TodoExternalId).NotEmpty();
    }
}
