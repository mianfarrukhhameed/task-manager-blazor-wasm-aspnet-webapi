using MediatR;
using System;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class ApplyAiPriorityCommand : IRequest<ApplyAiPriorityCommandResult>
{
    public Guid TodoExternalId { get; set; }
}
