using AutoMapper;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Dtos;
using Fistix.TaskManager.ViewModel.Queries.Todos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos
{
  public class GetAllTodoTasksQueryHandler : IRequestHandler<GetAllTodoTasksQuery, GetAllTodoTasksQueryResult>
  {
    private readonly IMapper _mapper;
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllTodoTasksQueryHandler(
      IMapper mapper,
      ITodoTaskRepository todoTaskRepository,
      ICurrentUserService currentUserService)
    {
      _mapper = mapper;
      _todoTaskRepository = todoTaskRepository;
      _currentUserService = currentUserService;
    }

    public async Task<GetAllTodoTasksQueryResult> Handle(GetAllTodoTasksQuery query, CancellationToken cancellationToken)
    {
      List<TodoTask> tasks;

      if (_currentUserService.HasAdminProfile)
      {
        tasks = await _todoTaskRepository.GetAll(cancellationToken);
      }
      else
      {
        var ownerId = TodoAccessGuard.RequireCurrentUserId(_currentUserService);
        tasks = await _todoTaskRepository.GetByOwner(ownerId, cancellationToken);
      }

      return new GetAllTodoTasksQueryResult()
      {
        Payload = _mapper.Map<List<TodoTaskDto>>(tasks)
      };
    }
  }
}