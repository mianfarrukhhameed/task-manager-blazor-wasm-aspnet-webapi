using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<TaskSummaryDto>> Summarize([FromBody] SummarizeTodoTaskCommand command)
    {
        try
        {
            _logger.LogInformation("Summarization request for todo {TodoExternalId}", command.TodoExternalId);

            if (command.TodoExternalId == Guid.Empty)
            {
                return BadRequest("A valid todoExternalId is required");
            }

            var result = await _mediator.Send(command);
            return Ok(result.Payload);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid summarization request for todo {TodoExternalId}", command.TodoExternalId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for todo {TodoExternalId}", command.TodoExternalId);
            return StatusCode(500, new { error = "Failed to generate summary", details = ex.Message });
        }
    }
}
