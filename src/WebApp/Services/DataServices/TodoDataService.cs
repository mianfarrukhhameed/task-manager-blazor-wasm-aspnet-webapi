using Fistix.TaskManager.ViewModel.Commands.Todos;
using Fistix.TaskManager.ViewModel.Dtos;
using Fistix.TaskManager.ViewModel.Queries.Todos;
using Fistix.TaskManager.WebApp.Extentions;
using Fistix.TaskManager.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApp.Services.DataServices
{
  public class TodoDataService
  {
    private HttpClient _httpClient;

    public TodoDataService(HttpClient httpClient)
    {
      _httpClient = httpClient;
    }

    public async Task<List<TodoTaskDto>> GetAll()
    {
      var result = await _httpClient.GetFromJsonAsync<GetAllTodoTasksQueryResult>("api/todos");
      return result.Payload;
    }

    public async Task<ApiCallResult<TodoTaskDto>> Post(CreateTodoTaskCommand command)
    {
      ApiCallResult<TodoTaskDto> result = new ApiCallResult<TodoTaskDto>();

      var response = await _httpClient.PostAsJsonAsync<CreateTodoTaskCommand>("api/todos", command);
      if (response.IsSuccessStatusCode)
      {
        result.Payload = await response.Content.ReadFromJsonAsync<TodoTaskDto>();
        result.IsSucceed = true;
      }
      else
      {
        result.IsSucceed = false;
        result.Message = await response.GetErrorMessage();
      }

      return result;
    }

    public async Task<ApiCallResult<TodoTaskDto>> Put(UpdateTodoTaskCommand command)
    {
      var result = new ApiCallResult<TodoTaskDto>();
      var response = await _httpClient.PutAsJsonAsync($"api/todos/{command.ExternalId}", command);

      if (response.IsSuccessStatusCode)
      {
        result.Payload = await response.Content.ReadFromJsonAsync<TodoTaskDto>();
        result.IsSucceed = true;
      }
      else
      {
        result.IsSucceed = false;
        result.Message = await response.GetErrorMessage();
      }

      return result;
    }

    public async Task<ApiCallResult<TaskSummaryDto>> Summarize(Guid todoExternalId, bool force = false)
    {
      var result = new ApiCallResult<TaskSummaryDto>();
      var command = new SummarizeTodoTaskCommand
      {
        TodoExternalId = todoExternalId,
        Force = force
      };

      var response = await _httpClient.PostAsJsonAsync("api/ai/summarize", command);
      if (response.IsSuccessStatusCode)
      {
        result.Payload = await response.Content.ReadFromJsonAsync<TaskSummaryDto>();
        result.IsSucceed = true;
      }
      else
      {
        result.IsSucceed = false;
        result.Message = await response.GetErrorMessage();
      }

      return result;
    }
  }
}
