using AutoMapper;
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
                .ForMember(up => up.CreatedOn, m => m.MapFrom(x => DateTime.Now));

      CreateMap<TodoTask, TodoTaskDto>()
        .ForMember(d => d.AiSummary, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.AiSummary : null))
        .ForMember(d => d.AiSummaryModel, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.AiSummaryModel : null))
        .ForMember(d => d.AiSummaryGeneratedAt, o => o.MapFrom(s => s.AiMetadata != null ? s.AiMetadata.UpdatedAt ?? s.AiMetadata.CreatedAt : (DateTime?)null));
    }
  }
}
