using Fistix.TaskManager.AiLayer.Implementations;
using Fistix.TaskManager.AiLayer.Models;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class SummarizeTodoTaskCommandHandler : IRequestHandler<SummarizeTodoTaskCommand, SummarizeTodoTaskCommandResult>
{
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ITodoAiMetadataRepository _todoAiMetadataRepository;
    private readonly SummarizationPipeline _summarizationPipeline;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SummarizeTodoTaskCommandHandler> _logger;

    public SummarizeTodoTaskCommandHandler(
        ITodoTaskRepository todoTaskRepository,
        ITodoAiMetadataRepository todoAiMetadataRepository,
        SummarizationPipeline summarizationPipeline,
        ICurrentUserService currentUserService,
        ILogger<SummarizeTodoTaskCommandHandler> logger)
    {
        _todoTaskRepository = todoTaskRepository;
        _todoAiMetadataRepository = todoAiMetadataRepository;
        _summarizationPipeline = summarizationPipeline;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SummarizeTodoTaskCommandResult> Handle(SummarizeTodoTaskCommand command, CancellationToken cancellationToken)
    {
        var todo = await _todoTaskRepository.Get(command.TodoExternalId, cancellationToken);

        TodoAccessGuard.EnsureCanAccess(todo, _currentUserService);

        if (string.IsNullOrWhiteSpace(todo.Title) || string.IsNullOrWhiteSpace(todo.Description))
        {
            throw new InvalidOperationException("Title and description are required to generate a summary.");
        }

        if (!command.Force)
        {
            var cachedMetadata = await _todoAiMetadataRepository.GetByTodoExternalIdAsync(command.TodoExternalId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cachedMetadata?.AiSummary))
            {
                _logger.LogInformation("Returning cached summary for todo {TodoExternalId}", command.TodoExternalId);

                return new SummarizeTodoTaskCommandResult
                {
                    Payload = new TaskSummaryDto
                    {
                        TodoExternalId = command.TodoExternalId,
                        Summary = cachedMetadata.AiSummary!,
                        Model = cachedMetadata.AiSummaryModel ?? string.Empty,
                        FromCache = true,
                        GeneratedAt = cachedMetadata.UpdatedAt ?? cachedMetadata.CreatedAt
                    }
                };
            }
        }

        var request = new SummarizationRequest
        {
            TodoExternalId = command.TodoExternalId,
            Title = todo.Title,
            Description = todo.Description,
            Force = command.Force
        };

        var response = await _summarizationPipeline.ExecuteAsync<SummarizationRequest, SummarizationResponse>(request);

        await _todoAiMetadataRepository.UpsertSummaryAsync(
            todo.Id,
            response.Summary,
            response.Model,
            cancellationToken);

        return new SummarizeTodoTaskCommandResult
        {
            Payload = new TaskSummaryDto
            {
                TodoExternalId = response.TodoExternalId,
                Summary = response.Summary,
                TokensUsed = response.TokensUsed,
                Model = response.Model,
                FromCache = false,
                GeneratedAt = response.GeneratedAt
            }
        };
    }
}
