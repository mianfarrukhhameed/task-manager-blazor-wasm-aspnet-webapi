using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Implementations;
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

public class ProposeAiToolsCommandHandler : IRequestHandler<ProposeAiToolsCommand, ProposeAiToolsCommandResult>
{
    private readonly ToolProposalPipeline _pipeline;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<ProposeAiToolsCommandHandler> _logger;

    public ProposeAiToolsCommandHandler(
        ToolProposalPipeline pipeline,
        AiConfiguration aiConfig,
        ILogger<ProposeAiToolsCommandHandler> logger)
    {
        _pipeline = pipeline;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    public async Task<ProposeAiToolsCommandResult> Handle(ProposeAiToolsCommand command, CancellationToken cancellationToken)
    {
        if (!_aiConfig.Features.EnableFunctionCalling)
        {
            throw new FeatureDisabledException("AI function calling");
        }

        _logger.LogInformation("Proposing AI tools for prompt length {Length}", command.Prompt?.Length ?? 0);

        var result = await _pipeline.ExecuteAsync(new ToolProposalPipelineRequest
        {
            Prompt = command.Prompt
        }, cancellationToken);

        return new ProposeAiToolsCommandResult
        {
            Payload = new ProposeAiToolsResponseDto
            {
                Explanation = result.Explanation,
                Model = result.Model,
                ProposedCalls = result.ProposedCalls.Select(c => new ProposedToolCallDto
                {
                    ToolName = c.ToolName,
                    Arguments = c.Arguments
                }).ToList()
            }
        };
    }
}
