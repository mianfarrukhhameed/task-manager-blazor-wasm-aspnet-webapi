using Fistix.TaskManager.AiLayer.Implementations;
using Fistix.TaskManager.AiLayer.Models;
using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.Exceptions;
using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class SemanticSearchTodosCommandHandler
    : IRequestHandler<SemanticSearchTodosCommand, SemanticSearchTodosCommandResult>
{
    private readonly SemanticSearchPipeline _pipeline;
    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<SemanticSearchTodosCommandHandler> _logger;

    public SemanticSearchTodosCommandHandler(
        SemanticSearchPipeline pipeline,
        ITodoTaskRepository todoTaskRepository,
        ICurrentUserService currentUserService,
        AiConfiguration aiConfig,
        ILogger<SemanticSearchTodosCommandHandler> logger)
    {
        _pipeline = pipeline;
        _todoTaskRepository = todoTaskRepository;
        _currentUserService = currentUserService;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public async Task<SemanticSearchTodosCommandResult> Handle(
        SemanticSearchTodosCommand command,
        CancellationToken cancellationToken)
    {
        if (!_aiConfig.Features.EnableSemanticSearch)
        {
            throw new FeatureDisabledException("AI semantic search");
        }

        if (!_aiConfig.Features.EnableEmbeddings)
        {
            throw new FeatureDisabledException("AI embeddings");
        }

        var currentUserId = TodoAccessGuard.RequireCurrentUserId(_currentUserService);
        var isAdmin = _currentUserService.HasAdminProfile;

        var pipelineResult = await _pipeline.ExecuteAsync(new SemanticSearchPipelineRequest
        {
            Query = command.Query,
            Limit = command.Limit,
            OwnerExternalId = isAdmin ? null : currentUserId
        }, cancellationToken);

        var results = new System.Collections.Generic.List<SemanticSearchHitDto>();
        foreach (var hit in pipelineResult.Hits)
        {
            try
            {
                var todo = await _todoTaskRepository.Get(hit.TodoExternalId, cancellationToken);
                if (!isAdmin)
                {
                    TodoAccessGuard.EnsureCanAccess(todo, _currentUserService);
                }

                results.Add(new SemanticSearchHitDto
                {
                    TodoExternalId = todo.ExternalId,
                    Title = todo.Title,
                    Description = todo.Description,
                    Priority = todo.Priority,
                    Status = todo.Status,
                    Similarity = hit.Similarity
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping semantic hit {TodoExternalId}", hit.TodoExternalId);
            }
        }

        return new SemanticSearchTodosCommandResult
        {
            Payload = new SemanticSearchResponseDto
            {
                Results = results,
                ExecutionTimeMs = pipelineResult.ExecutionTimeMs,
                Model = pipelineResult.Model
            }
        };
    }
}
