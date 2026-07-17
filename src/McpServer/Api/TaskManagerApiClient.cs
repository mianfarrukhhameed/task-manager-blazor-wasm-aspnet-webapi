using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Fistix.TaskManager.McpServer.Auth;
using Fistix.TaskManager.McpServer.Configuration;

namespace Fistix.TaskManager.McpServer.Api;

public sealed class TaskManagerApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IAccessTokenProvider _tokenProvider;

    public TaskManagerApiClient(
        HttpClient httpClient,
        McpServerOptions options,
        IAccessTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _httpClient.BaseAddress = new Uri(options.ApiUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IReadOnlyList<TodoItem>> GetTodosAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/todos", content: null, cancellationToken);
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

        using var response = await SendJsonAsync(HttpMethod.Post, "api/todos", request, cancellationToken);
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

        using var response = await SendJsonAsync(
            HttpMethod.Put,
            $"api/todos/{externalId}",
            request,
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

        using var response = await SendJsonAsync(
            HttpMethod.Post,
            "api/ai/todos/search/semantic",
            request,
            cancellationToken);

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

    private Task<HttpResponseMessage> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        T body,
        CancellationToken cancellationToken) =>
        SendAsync(
            method,
            path,
            () => new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json"),
            cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        HttpContent? content,
        CancellationToken cancellationToken) =>
        await SendAsync(method, path, content is null ? null : () => content, cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        Func<HttpContent>? contentFactory,
        CancellationToken cancellationToken)
    {
        var response = await SendOnceAsync(method, path, contentFactory, forceRefresh: false, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        return await SendOnceAsync(method, path, contentFactory, forceRefresh: true, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string path,
        Func<HttpContent>? contentFactory,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(forceRefresh, cancellationToken);
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (contentFactory is not null)
        {
            request.Content = contentFactory();
        }

        return await _httpClient.SendAsync(request, cancellationToken);
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
