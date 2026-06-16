using MediatR;
using System;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class SummarizeTodoTaskCommand : IRequest<SummarizeTodoTaskCommandResult>
{
    public Guid TodoExternalId { get; set; }
    public bool Force { get; set; }
}
