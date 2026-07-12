using MediatR;
using System;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class ClassifyTodoTaskCommand : IRequest<ClassifyTodoTaskCommandResult>
{
    public Guid TodoExternalId { get; set; }
    public bool Force { get; set; }
}
