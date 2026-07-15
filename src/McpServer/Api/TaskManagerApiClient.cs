using System.Net;
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

    public async Task<TodoItem> GetTodoAsync(Guid externalId, CancellationToken cancellationToken = default)
    {
        var todos = await GetTodosAsync(cancellationToken);
        var todo = todos.FirstOrDefault(t => t.ExternalId == externalId);
        return todo
            ?? throw new InvalidOperationException($"Todo '{externalId}' was not found for the authenticated user.");
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
        Guid externalId,
        string title,
        string description,
        DateTime dueDate,
        string priority,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateTodoRequest
        {
            ExternalId = externalId,
            Title = title,
            Description = description,
            DueDate = dueDate,
            Priority = priority
        };

        using var response = await _httpClient.PutAsJsonAsync(
            $"api/todos/{externalId}",
            request,
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var updated = await response.Content.ReadFromJsonAsync<TodoItem>(JsonOptions, cancellationToken);
        return updated ?? throw new InvalidOperationException("Update todo returned an empty body.");
    }

    public async Task<SemanticSearchAttempt> TrySemanticSearchAsync(
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

        // Only soft-fallback when the feature is unavailable or rate-limited.
        // Validation/auth errors (400/401/403) propagate as tool errors.
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            return new SemanticSearchAttempt { SoftFallback = true };
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new SemanticSearchAttempt { SoftFallback = true, RateLimited = true };
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<SemanticSearchResponse>(JsonOptions, cancellationToken);
        return new SemanticSearchAttempt { Response = payload };
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
