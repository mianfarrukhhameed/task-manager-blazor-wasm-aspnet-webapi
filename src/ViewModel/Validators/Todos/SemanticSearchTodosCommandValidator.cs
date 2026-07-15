using Fistix.TaskManager.ViewModel.Commands.Todos;
using FluentValidation;

namespace Fistix.TaskManager.ViewModel.Validators.Todos;

public class SemanticSearchTodosCommandValidator : AbstractValidator<SemanticSearchTodosCommand>
{
    public SemanticSearchTodosCommandValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
    }
}
