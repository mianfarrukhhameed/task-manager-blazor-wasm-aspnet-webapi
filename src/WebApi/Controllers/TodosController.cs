using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using Fistix.TaskManager.ViewModel.Queries.Todos;
using Fistix.TaskManager.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize]
  public class TodosController : ControllerBase
  {
    private readonly IMediator _mediator;
    private readonly ILogger<TodosController> _logger;

    public TodosController(IMediator mediator, ILogger<TodosController> logger)
    {
      _mediator = mediator;
      _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTodoTaskCommand command)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      try
      {       
        var result = await _mediator.Send(command);

        return Created($"api/todos/{result.Payload.ExternalId}", result.Payload);
      }
      catch (BadHttpRequestException ex)
      {
        return BadRequest(new ProblemDetails()
        {
          Detail = ex.Message
        });
      }      
      catch (InvalidOperationException ex)
      {
        return Conflict(new ProblemDetails()
        {
          Detail = ex.Message
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating todo task");
        return ApiErrorResponses.UnexpectedError(HttpContext, "Failed to create task");
      }
    }

    [HttpGet]
    [Authorize(PolicyNames.IsAdmin)]
    [ProducesResponseType(typeof(GetAllTodoTasksQueryResult), StatusCodes.Status200OK)]    
    public async Task<IActionResult> GetAllTodoTasks()
    {      
      var result = await _mediator.Send(new GetAllTodoTasksQuery());

      return Ok(result);
    }

    [HttpPut("{externalId}")]
    [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(Guid externalId, [FromBody] UpdateTodoTaskCommand command)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (externalId != command.ExternalId)
        return BadRequest(new ProblemDetails { Detail = "Route id does not match request body." });

      try
      {
        var result = await _mediator.Send(command);
        return Ok(result.Payload);
      }
      catch (Core.Exceptions.NotFoundException)
      {
        return NotFound();
      }
      catch (BadHttpRequestException ex)
      {
        return BadRequest(new ProblemDetails { Detail = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating todo task {ExternalId}", externalId);
        return ApiErrorResponses.UnexpectedError(HttpContext, "Failed to update task");
      }
    }
  }
}