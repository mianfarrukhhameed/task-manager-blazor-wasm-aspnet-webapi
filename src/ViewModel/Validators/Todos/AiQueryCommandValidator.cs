using Fistix.TaskManager.ViewModel.Commands.Todos;
using FluentValidation;
using System;
using System.Linq;

namespace Fistix.TaskManager.ViewModel.Validators.Todos;

public class AiQueryCommandValidator : AbstractValidator<AiQueryCommand>
{
    private static readonly string[] AllowedContexts = ["week", "project", "workload"];

    public AiQueryCommandValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Context)
            .NotEmpty()
            .Must(c => AllowedContexts.Contains(c.Trim().ToLowerInvariant()))
            .WithMessage("Context must be one of: week, project, workload");
    }
}
