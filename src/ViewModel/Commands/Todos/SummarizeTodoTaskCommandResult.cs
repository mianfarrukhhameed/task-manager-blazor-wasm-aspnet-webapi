using Fistix.TaskManager.ViewModel.Dtos;

namespace Fistix.TaskManager.ViewModel.Commands.Todos;

public class SummarizeTodoTaskCommandResult
{
    public TaskSummaryDto Payload { get; set; } = new();
}
