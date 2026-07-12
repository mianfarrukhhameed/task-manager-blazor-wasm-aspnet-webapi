using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.Exceptions;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class ClassifyTodoTaskCommandHandler : IRequestHandler<ClassifyTodoTaskCommand, ClassifyTodoTaskCommandResult>
{
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly IClassificationProcessor _classificationProcessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<ClassifyTodoTaskCommandHandler> _logger;

    public ClassifyTodoTaskCommandHandler(
        ITodoTaskRepository todoTaskRepository,
        IClassificationProcessor classificationProcessor,
        ICurrentUserService currentUserService,
        AiConfiguration aiConfig,
        ILogger<ClassifyTodoTaskCommandHandler> logger)
    {
        _todoTaskRepository = todoTaskRepository;
        _classificationProcessor = classificationProcessor;
        _currentUserService = currentUserService;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public async Task<ClassifyTodoTaskCommandResult> Handle(ClassifyTodoTaskCommand command, CancellationToken cancellationToken)
    {
        if (!_aiConfig.Features.EnableClassification)
        {
            throw new FeatureDisabledException("AI classification");
        }

        var todo = await _todoTaskRepository.Get(command.TodoExternalId, cancellationToken);
        TodoAccessGuard.EnsureCanAccess(todo, _currentUserService);

        _logger.LogDebug("Classification retry for todo {TodoExternalId}, force={Force}",
            command.TodoExternalId, command.Force);

        var payload = await _classificationProcessor.ProcessAsync(command.TodoExternalId, command.Force, cancellationToken);

        return new ClassifyTodoTaskCommandResult { Payload = payload };
    }
}
