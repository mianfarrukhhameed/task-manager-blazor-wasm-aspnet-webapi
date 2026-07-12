using Fistix.TaskManager.ViewModel.Commands.Todos;
using FluentValidation;

namespace Fistix.TaskManager.ViewModel.Validators.Todos;

public class ApplyAiPriorityCommandValidator : AbstractValidator<ApplyAiPriorityCommand>
{
    public ApplyAiPriorityCommandValidator()
    {
        RuleFor(x => x.TodoExternalId).NotEmpty();
    }
}
