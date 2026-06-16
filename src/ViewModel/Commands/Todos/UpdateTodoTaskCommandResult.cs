using Fistix.TaskManager.ViewModel.Dtos;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class UpdateTodoTaskCommandResult
{
    public TodoTaskDto Payload { get; set; } = new();
}
