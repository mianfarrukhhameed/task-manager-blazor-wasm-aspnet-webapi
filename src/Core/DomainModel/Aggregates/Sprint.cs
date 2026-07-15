#nullable enable

using Fistix.TaskManager.Core.DomainModel.SeedWork;
using System;
using System.Collections.Generic;

namespace Fistix.TaskManager.Core.DomainModel.Aggregates;

public class Sprint : Entity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Reasoning { get; set; }
    public virtual ICollection<SprintTodo> SprintTodos { get; set; } = new List<SprintTodo>();
}
