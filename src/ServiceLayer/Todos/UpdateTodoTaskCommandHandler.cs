using AutoMapper;
using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ServiceLayer.Background;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class UpdateTodoTaskCommandHandler : IRequestHandler<UpdateTodoTaskCommand, UpdateTodoTaskCommandResult>
{
    private readonly IMapper _mapper;
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ITodoAiMetadataRepository _todoAiMetadataRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmbeddingQueue _embeddingQueue;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<UpdateTodoTaskCommandHandler> _logger;

    public UpdateTodoTaskCommandHandler(
        IMapper mapper,
        ITodoTaskRepository todoTaskRepository,
        ITodoAiMetadataRepository todoAiMetadataRepository,
        ICurrentUserService currentUserService,
        IEmbeddingQueue embeddingQueue,
        AiConfiguration aiConfig,
        ILogger<UpdateTodoTaskCommandHandler> logger)
    {
        _mapper = mapper;
        _todoTaskRepository = todoTaskRepository;
        _todoAiMetadataRepository = todoAiMetadataRepository;
        _currentUserService = currentUserService;
        _embeddingQueue = embeddingQueue;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public async Task<UpdateTodoTaskCommandResult> Handle(UpdateTodoTaskCommand command, CancellationToken cancellationToken)
    {
        var todoTask = await _todoTaskRepository.Get(command.ExternalId, cancellationToken);

        TodoAccessGuard.EnsureCanAccess(todoTask, _currentUserService);

        todoTask.Title = command.Title;
        todoTask.Description = command.Description;
        todoTask.DueDate = command.DueDate;
        todoTask.Priority = ClassificationGuardrails.ToTaskPriority(command.Priority);

        await _todoTaskRepository.Update(todoTask, cancellationToken);

        var metadata = await _todoAiMetadataRepository.GetByTodoExternalIdAsync(command.ExternalId, cancellationToken);
        if (metadata is not null && !string.IsNullOrWhiteSpace(metadata.AiPriority))
        {
            var wasOverridden = ClassificationGuardrails.IsPriorityOverridden(
                todoTask.Priority,
                metadata.AiPriority);
            await _todoAiMetadataRepository.SetWasOverriddenAsync(todoTask.Id, wasOverridden, cancellationToken);
        }

        if (_aiConfig.Features.EnableEmbeddings)
        {
            await _embeddingQueue.EnqueueAsync(todoTask.ExternalId, cancellationToken);
            _logger.LogInformation("Queued background embedding refresh for todo {TodoExternalId}", todoTask.ExternalId);
        }

        todoTask = await _todoTaskRepository.Get(command.ExternalId, cancellationToken);

        return new UpdateTodoTaskCommandResult
        {
            Payload = _mapper.Map<TodoTaskDto>(todoTask)
        };
    }
}
