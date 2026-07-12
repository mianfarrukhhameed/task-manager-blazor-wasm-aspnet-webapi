using Fistix.TaskManager.ViewModel.Dtos;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class ClassifyTodoTaskCommandResult
{
    public TaskClassificationDto Payload { get; set; } = new();
}
