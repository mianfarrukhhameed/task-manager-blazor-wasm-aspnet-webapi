#nullable enable

using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using System;

namespace Fistix.TaskManager.ViewModel.Queries.Todos;

public class GetSprintQuery : IRequest<GetSprintQueryResult>
{
    public Guid ExternalId { get; set; }
}

public class GetSprintQueryResult
{
    public SprintDto Payload { get; set; } = new();
}
