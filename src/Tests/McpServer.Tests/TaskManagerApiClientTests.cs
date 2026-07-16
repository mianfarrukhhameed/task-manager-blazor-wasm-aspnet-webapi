using System.Net;
using System.Text;
using Fistix.TaskManager.McpServer.Api;
using Fistix.TaskManager.McpServer.Configuration;
using Xunit;

namespace Fistix.TaskManager.McpServer.Tests;

public class TaskManagerApiClientTests
{
    [Fact]
    public async Task GetTodosAsync_ReadsPayloadEnvelope()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var json = """
            {
              "payload": [
                {
                  "externalId": "11111111-1111-1111-1111-111111111111",
                  "title": "Pay invoice",
                  "description": "Q3 billing",
                  "dueDate": "2026-07-20T00:00:00Z",
                  "status": 1,
                  "priority": "High"
                }
              ]
            }
            """;

        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handler);
        var todos = await client.GetTodosAsync();

        Assert.Single(todos);
        Assert.Equal(id, todos[0].ExternalId);
        Assert.Equal("Pay invoice", todos[0].Title);
        Assert.Equal("High", todos[0].Priority);
        Assert.Equal(1, todos[0].Status);
        Assert.Contains("api/todos", handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task CreateTodoAsync_ReadsBareTodoDto()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var json = """
            {
              "externalId": "22222222-2222-2222-2222-222222222222",
              "title": "New task",
              "description": "Created via MCP",
              "dueDate": "2026-07-25T00:00:00Z",
              "status": 1,
              "priority": "Medium"
            }
            """;

        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handler);
        var created = await client.CreateTodoAsync("New task", "Created via MCP", new DateTime(2026, 7, 25));

        Assert.Equal(id, created.ExternalId);
        Assert.Equal("New task", created.Title);
        Assert.Equal("Medium", created.Priority);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
    }

    [Fact]
    public async Task TrySemanticSearchAsync_SoftFallsBack_On503And429()
    {
        var handler503 = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var attempt503 = await CreateClient(handler503).TrySemanticSearchAsync("payment");
        Assert.True(attempt503.SoftFallback);
        Assert.False(attempt503.RateLimited);
        Assert.Null(attempt503.Response);

        var handler429 = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var attempt429 = await CreateClient(handler429).TrySemanticSearchAsync("payment");
        Assert.True(attempt429.SoftFallback);
        Assert.True(attempt429.RateLimited);
    }

    [Fact]
    public async Task TrySemanticSearchAsync_Throws_OnBadRequest()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"detail":"query required"}""", Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.TrySemanticSearchAsync("x"));
    }

    private static TaskManagerApiClient CreateClient(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        var options = new McpServerOptions
        {
            ApiUrl = "http://localhost:5000",
            AccessToken = "test-token"
        };
        return new TaskManagerApiClient(http, options);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}
