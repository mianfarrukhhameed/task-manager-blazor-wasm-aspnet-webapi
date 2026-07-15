using Fistix.TaskManager.ViewModel.Commands.Todos;
using FluentValidation;

namespace Fistix.TaskManager.ViewModel.Validators.Todos;

public class ProposeAiToolsCommandValidator : AbstractValidator<ProposeAiToolsCommand>
{
    public ProposeAiToolsCommandValidator()
    {
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(2000);
    }
}
