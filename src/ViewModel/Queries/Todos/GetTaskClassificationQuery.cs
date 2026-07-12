using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using System;

namespace Fistix.TaskManager.ViewModel.Queries.Todos;

public class GetTaskClassificationQuery : IRequest<GetTaskClassificationQueryResult>
{
    public Guid TodoExternalId { get; set; }
}

public class GetTaskClassificationQueryResult
{
    public TaskClassificationDto Payload { get; set; } = new();
}
