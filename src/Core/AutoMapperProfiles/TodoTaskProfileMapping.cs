using AutoMapper;
using Fistix.TaskManager.Core;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using System;

namespace Fistix.TaskManager.Core.AutoMapperProfiles
{
  public class TodoTaskProfileMapping : Profile
  {
    public TodoTaskProfileMapping()
    {
      CreateMap<CreateTodoTaskCommand, TodoTask>()
                .ForMember(up => up.Id, m => m.Ignore())
                .ForMember(up => up.DueDate, m => m.MapFrom(x => DateTimeUtc.EnsureUtc(x.DueDate)))
                .ForMember(up => up.CreatedOn, m => m.MapFrom(x => DateTime.UtcNow));

      CreateMap<TodoTask, TodoTaskDto>()
        .ForMember(d => d.AiSummary, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.AiSummary : null))
        .ForMember(d => d.AiSummaryModel, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.AiSummaryModel : null))
        .ForMember(d => d.AiSummaryGeneratedAt, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.UpdatedAt ?? s.AiMetadata.CreatedAt : (DateTime?)null))
        .ForMember(d => d.AiSuggestedPriority, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.AiPriority : null))
        .ForMember(d => d.AiPriorityConfidence, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.ConfidenceScore : null))
        .ForMember(d => d.AiPriorityReason, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.AiPriorityReason : null))
        .ForMember(d => d.ClassificationStatus, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.ClassificationStatus : null))
        .ForMember(d => d.AiPriorityModel, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.AiPriorityModel : null))
        .ForMember(d => d.AiPriorityGeneratedAt, o => o.MapFrom(s =>
            s.AiMetadata != null && !string.IsNullOrWhiteSpace(s.AiMetadata.AiPriority)
                ? s.AiMetadata.UpdatedAt ?? s.AiMetadata.CreatedAt
                : (DateTime?)null));
    }
  }
}
