using AutoMapper;
using Fistix.TaskManager.Core.Abstractions.Repositories;
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

    public UpdateTodoTaskCommandHandler(IMapper mapper, ITodoTaskRepository todoTaskRepository)
    {
        _mapper = mapper;
        _todoTaskRepository = todoTaskRepository;
    }

    public async Task<UpdateTodoTaskCommandResult> Handle(UpdateTodoTaskCommand command, CancellationToken cancellationToken)
    {
        var todoTask = await _todoTaskRepository.Get(command.ExternalId, cancellationToken);

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
