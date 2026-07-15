#nullable enable

using System;
using System.Collections.Generic;

namespace Fistix.TaskManager.ViewModel.Dtos;

public class SprintDto
{
    public Guid ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Reasoning { get; set; }
    public List<Guid> TodoExternalIds { get; set; } = new();
    public List<SprintTaskSummaryDto> Tasks { get; set; } = new();
}

public class SprintTaskSummaryDto
{
    public Guid ExternalId { get; set; }
    public string? Title { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Category { get; set; }
}
