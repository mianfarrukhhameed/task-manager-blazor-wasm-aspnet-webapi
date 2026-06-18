using Fistix.TaskManager.Core.Exceptions;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using Fistix.TaskManager.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApi.Controllers;

/// <summary>
/// API endpoints for AI features (summarization, classification, etc).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AiController> _logger;

    public AiController(IMediator mediator, ILogger<AiController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generates an AI summary for a task description.
    /// Title and description are loaded from the database; only the task id and force flag are required.
    /// </summary>
    [HttpPost("summarize")]
    [ProducesResponseType(typeof(TaskSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<TaskSummaryDto>> Summarize([FromBody] SummarizeTodoTaskCommand command)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Summarization request for todo {TodoExternalId}", command.TodoExternalId);

            var result = await _mediator.Send(command);
            return Ok(result.Payload);
        }
        catch (FeatureDisabledException ex)
        {
            _logger.LogWarning(ex, "Summarization feature disabled for todo {TodoExternalId}", command.TodoExternalId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "AI summarization is unavailable",
                Detail = ex.Message,
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid summarization request for todo {TodoExternalId}", command.TodoExternalId);
            return BadRequest(ex.Message);
        }
        catch (ForbiddenAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for todo {TodoExternalId}", command.TodoExternalId);
            return ApiErrorResponses.UnexpectedError(HttpContext, "Failed to generate summary");
        }
    }
}
