#nullable enable

using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.Exceptions;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Queries.Todos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class GetSprintQueryHandler : IRequestHandler<GetSprintQuery, GetSprintQueryResult>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetSprintQueryHandler(
        ISprintRepository sprintRepository,
        ICurrentUserService currentUserService)
    {
        _sprintRepository = sprintRepository;
        _currentUserService = currentUserService;
    }

    public async Task<GetSprintQueryResult> Handle(GetSprintQuery request, CancellationToken cancellationToken)
    {
        var userId = TodoAccessGuard.RequireCurrentUserId(_currentUserService);
        var sprint = await _sprintRepository.Get(request.ExternalId, cancellationToken);

        if (!_currentUserService.HasAdminProfile && sprint.CreatedByUserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        return new GetSprintQueryResult
        {
            Payload = CreateSprintCommandHandler.MapSprint(sprint)
        };
    }
}
