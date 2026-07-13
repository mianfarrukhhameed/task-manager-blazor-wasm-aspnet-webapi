#nullable enable

using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Fistix.TaskManager.ViewModel.Queries.Todos;

public class GetSprintsQuery : IRequest<GetSprintsQueryResult>
{
}

public class GetSprintsQueryResult
{
    public List<SprintDto> Payload { get; set; } = new();
}
