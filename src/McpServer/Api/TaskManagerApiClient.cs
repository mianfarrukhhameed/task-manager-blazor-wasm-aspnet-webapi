using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fistix.TaskManager.McpServer.Configuration;

namespace Fistix.TaskManager.McpServer.Api;

public sealed class TaskManagerApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public TaskManagerApiClient(HttpClient httpClient, McpServerOptions options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.ApiUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.AccessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IReadOnlyList<TodoItem>> GetTodosAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/todos", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<TodoListResponse>(JsonOptions, cancellationToken);
        return body?.Payload ?? [];
    }

    public async Task<TodoItem> CreateTodoAsync(
        string title,
        string description,
        DateTime dueDate,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateTodoRequest
        {
            Title = title,
            Description = description,
            DueDate = dueDate
        };

        using var response = await _httpClient.PostAsJsonAsync("api/todos", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var created = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions, cancellationToken);
        return created ?? throw new InvalidOperationException("Create todo returned an empty body.");
    }

    public async Task<TodoItem> UpdateTodoAsync(
        TodoItem todo,
        string priority,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateTodoRequest
        {
            ExternalId = todo.ExternalId,
            Title = todo.Title ?? string.Empty,
            Description = todo.Description ?? string.Empty,
            DueDate = todo.DueDate ?? DateTime.UtcNow.Date,
            Priority = priority
        };

        using var response = await _httpClient.PutAsJsonAsync(
            $"api/todos/{todo.ExternalId}",
            request,
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var updated = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions, cancellationToken);
        return updated ?? throw new InvalidOperationException("Update todo returned an empty body.");
    }

    public async Task<SemanticSearchResponse?> TrySemanticSearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var request = new SemanticSearchRequest { Query = query, Limit = limit };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/ai/todos/search/semantic",
            request,
            JsonOptions,
            cancellationToken);

        if (response.StatusCode is System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<SemanticSearchResponse>(JsonOptions, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Task Manager API returned {(int)response.StatusCode} {response.ReasonPhrase}. {detail}");
    }
}
