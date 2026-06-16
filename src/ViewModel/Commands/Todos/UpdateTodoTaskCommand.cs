using MediatR;
using System;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class UpdateTodoTaskCommand : IRequest<UpdateTodoTaskCommandResult>
{
    public Guid ExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
