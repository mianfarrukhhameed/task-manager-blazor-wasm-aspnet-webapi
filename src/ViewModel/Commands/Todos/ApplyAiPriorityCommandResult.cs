using Fistix.TaskManager.ViewModel.Dtos;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class ApplyAiPriorityCommandResult
{
    public TodoTaskDto Payload { get; set; } = new();
}
