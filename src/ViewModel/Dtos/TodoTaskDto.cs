using System;
using System.Collections.Generic;
using System.Text;
using Fistix.TaskManager.ViewModel.Enums;

namespace Fistix.TaskManager.ViewModel.Dtos
{
  public class TodoTaskDto
  {
    public Guid ExternalId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TodoTaskStatus Status { get; set; }
    public string? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public string? Category { get; set; }
    public string? AiSummary { get; set; }
    public string? AiSummaryModel { get; set; }
    public DateTime? AiSummaryGeneratedAt { get; set; }
  }
}
