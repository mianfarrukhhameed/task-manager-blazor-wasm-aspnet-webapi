#nullable enable

namespace Fistix.TaskManager.Core.DomainModel.Aggregates;

public class SprintTodo
{
    public int SprintId { get; set; }
    public int TodoId { get; set; }
    public virtual Sprint? Sprint { get; set; }
    public virtual TodoTask? TodoTask { get; set; }
}
