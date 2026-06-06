using Fistix.TaskManager.AiLayer.Implementations;
using Fistix.TaskManager.AiLayer.Models;
using Fistix.TaskManager.ViewModel.Dtos;
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
    private readonly SummarizationPipeline _summarizationPipeline;
    private readonly ILogger<AiController> _logger;

    public AiController(
        SummarizationPipeline summarizationPipeline,
        ILogger<AiController> logger)
    {
        _summarizationPipeline = summarizationPipeline;
        _logger = logger;
    }

    /// <summary>
    /// Generates an AI summary for a task description.
    /// </summary>
    /// <param name="todoExternalId">The external ID of the task to summarize</param>
    /// <param name="title">Task title</param>
    /// <param name="description">Task description to summarize</param>
    /// <param name="force">Force regeneration even if cached</param>
    /// <returns>Task summary and metadata</returns>
    [HttpPost("summarize")]
    public async Task<ActionResult<TaskSummaryDto>> Summarize(
        [FromQuery] Guid todoExternalId,
        [FromQuery] string title,
        [FromQuery] string description,
        [FromQuery] bool force = false)
    {
        try
        {
            _logger.LogInformation("Summarization request for todo {TodoExternalId}", todoExternalId);

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                return BadRequest("Title and description are required");
            }

            var request = new SummarizationRequest
            {
                TodoExternalId = todoExternalId,
                Title = title,
                Description = description,
                Force = force
            };

            var response = await _summarizationPipeline.ExecuteAsync<SummarizationRequest, SummarizationResponse>(request);

            var dto = new TaskSummaryDto
            {
                TodoExternalId = response.TodoExternalId,
                Summary = response.Summary,
                TokensUsed = response.TokensUsed,
                Model = response.Model,
                FromCache = response.FromCache,
                GeneratedAt = response.GeneratedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for todo {TodoExternalId}", todoExternalId);
            return StatusCode(500, new { error = "Failed to generate summary", details = ex.Message });
        }
    }
}
