using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Models;
using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.Core.Exceptions;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Todos;

public class ExecuteAiToolsCommandHandler : IRequestHandler<ExecuteAiToolsCommand, ExecuteAiToolsCommandResult>
{
    private readonly IToolExecutor _toolExecutor;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<ExecuteAiToolsCommandHandler> _logger;

    public ExecuteAiToolsCommandHandler(
        IToolExecutor toolExecutor,
        AiConfiguration aiConfig,
        ILogger<ExecuteAiToolsCommandHandler> logger)
    {
        _toolExecutor = toolExecutor;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public async Task<ExecuteAiToolsCommandResult> Handle(ExecuteAiToolsCommand command, CancellationToken cancellationToken)
    {
        if (!_aiConfig.Features.EnableFunctionCalling)
        {
            throw new FeatureDisabledException("AI function calling");
        }

        _logger.LogInformation("Executing {Count} confirmed AI tool call(s)", command.ConfirmedCalls.Count);

        var calls = command.ConfirmedCalls.Select(c => new ProposedToolCall
        {
            ToolName = c.ToolName,
            Arguments = c.Arguments
        }).ToList();

        var outcomes = await _toolExecutor.ExecuteAsync(calls, cancellationToken);

        return new ExecuteAiToolsCommandResult
        {
            Payload = new ExecuteAiToolsResponseDto
            {
                Results = outcomes.Select(o => new ToolExecutionResultDto
                {
                    ToolName = o.ToolName,
                    Success = o.Success,
                    Message = o.Message,
                    ResultJson = o.ResultJson
                }).ToList()
            }
        };
    }
}
