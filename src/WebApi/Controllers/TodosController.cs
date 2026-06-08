using Fistix.TaskManager.Core.SecurityModel;
using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using Fistix.TaskManager.ViewModel.Queries.Todos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace Fistix.TaskManager.WebApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize]
  public class TodosController : ControllerBase
  {
    private readonly IMediator _mediator = null;

    public TodosController(IMediator mediator)
    {
      _mediator = mediator;
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
    }

    [HttpGet]
    [Authorize(PolicyNames.IsAdmin)]
    [ProducesResponseType(typeof(GetAllTodoTasksQueryResult), StatusCodes.Status200OK)]    
    public async Task<IActionResult> GetAllTodoTasks()
    {      
      var result = await _mediator.Send(new GetAllTodoTasksQuery());

      return Ok(result);
    }

    // Vulnerable endpoint for code scanning tests
    // Contains multiple insecure patterns intentionally:
    // - SQL string concatenation (possible SQL injection)
    // - MD5 hashing (weak crypto)
    // - BinaryFormatter deserialization (insecure deserialization)
    // - Disabling TLS validation (insecure HTTP client)
    // - Blocking on async call (sync-over-async)
    // - Predictable temp file path
    // - Process.Start with user input (command injection risk)
    [HttpPost("vuln-test")]
    [AllowAnonymous]
    public IActionResult VulnerableTest([FromBody] VulnerableInput input)
    {
      // SQL injection pattern (do NOT execute in production)
      var sql = "SELECT * FROM Users WHERE Name = '" + (input?.Username ?? "") + "'";

      // Weak hashing
      byte[] md5Hash;
      using (var md5 = MD5.Create())
      {
        md5Hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input?.Password ?? string.Empty));
      }

      // Insecure deserialization
      object deserialized = null;
      try
      {
        var bf = new BinaryFormatter();
        using (var ms = new MemoryStream(input?.SerializedPayload ?? Array.Empty<byte>()))
        {
          deserialized = bf.Deserialize(ms);
        }
      }
      catch { /* swallow for test */ }

      // Insecure HTTP client with TLS validation disabled
      using (var handler = new HttpClientHandler())
      {
        handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
        using (var http = new HttpClient(handler))
        {
          // Blocking call (sync-over-async)
          try { var resp = http.GetAsync(input?.Url ?? "https://example.com").Result; }
          catch { }
        }
      }

      // Predictable temp file path
      var usernamePart = input?.Username ?? "user";
      var invalidChars = Path.GetInvalidFileNameChars();
      var safeUsername = new string(usernamePart
        .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
        .ToArray())
        .Replace("/", "_")
        .Replace("\\", "_")
        .Replace("..", "_");
      if (string.IsNullOrWhiteSpace(safeUsername))
      {
        safeUsername = "user";
      }
      var tempPath = Path.Combine(Path.GetTempPath(), "app_temp_" + safeUsername + ".tmp");
      try { System.IO.File.WriteAllText(tempPath, "test"); } catch { }

      // Dangerous process start
      if (!string.IsNullOrEmpty(input?.Command))
      {
        try { Process.Start(input.Command); } catch { }
      }

      return Ok(new { sql, md5 = Convert.ToBase64String(md5Hash ?? Array.Empty<byte>()), deserialized = deserialized?.ToString(), tempPath });
    }

    public class VulnerableInput
    {
      public string Username { get; set; }
      public string Password { get; set; }
      public string Command { get; set; }
      public byte[] SerializedPayload { get; set; }
      public string Url { get; set; }
    }
  }
}