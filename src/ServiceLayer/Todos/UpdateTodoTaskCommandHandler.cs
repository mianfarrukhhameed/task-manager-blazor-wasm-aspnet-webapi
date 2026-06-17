using AutoMapper;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class UpdateTodoTaskCommandHandler : IRequestHandler<UpdateTodoTaskCommand, UpdateTodoTaskCommandResult>
{
    private readonly IMapper _mapper;
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTodoTaskCommandHandler(
        IMapper mapper,
        ITodoTaskRepository todoTaskRepository,
        ICurrentUserService currentUserService)
    {
        _mapper = mapper;
        _todoTaskRepository = todoTaskRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateTodoTaskCommandResult> Handle(UpdateTodoTaskCommand command, CancellationToken cancellationToken)
    {
        var todoTask = await _todoTaskRepository.Get(command.ExternalId, cancellationToken);

        TodoAccessGuard.EnsureCanAccess(todoTask, _currentUserService);

        todoTask.Title = command.Title;
        todoTask.Description = command.Description;
        todoTask.DueDate = command.DueDate;

        await _todoTaskRepository.Update(todoTask, cancellationToken);
        todoTask = await _todoTaskRepository.Get(command.ExternalId, cancellationToken);

        return new UpdateTodoTaskCommandResult
        {
            Payload = _mapper.Map<TodoTaskDto>(todoTask)
        };
    }
}
