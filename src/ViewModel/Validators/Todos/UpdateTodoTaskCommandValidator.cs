using Fistix.TaskManager.ViewModel.Commands.Todos;
using FluentValidation;
using System;

namespace Fistix.TaskManager.ViewModel.Validators.Todos;

public class UpdateTodoTaskCommandValidator : AbstractValidator<UpdateTodoTaskCommand>
{
    public UpdateTodoTaskCommandValidator()
    {
        RuleFor(x => x.ExternalId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(TodoFieldLimits.TitleMaxLength);
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(TodoFieldLimits.DescriptionMaxLength);
        RuleFor(x => x.DueDate)
            .NotEmpty()
            .GreaterThan(DateTime.Now)
            .WithMessage("Due Date should be future date");
    }
}
