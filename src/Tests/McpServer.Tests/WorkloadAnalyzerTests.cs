using Fistix.TaskManager.McpServer.Api;
using Fistix.TaskManager.McpServer.Auth;
using Fistix.TaskManager.McpServer.Configuration;
using Fistix.TaskManager.McpServer.Services;
using Xunit;

namespace Fistix.TaskManager.McpServer.Tests;

public class WorkloadAnalyzerTests
{
    [Fact]
    public void Analyze_ComputesOverdueActiveAndDueThisWeek()
    {
        var now = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
        var todos = new List<TodoItem>
        {
            new()
            {
                Title = "Overdue high",
                Priority = "High",
                Status = 1,
                DueDate = now.Date.AddDays(-2)
            },
            new()
            {
                Title = "Due tomorrow",
                Priority = "Medium",
                Status = 2,
                DueDate = now.Date.AddDays(1)
            },
            new()
            {
                Title = "Completed old",
                Priority = "Low",
                Status = 3,
                DueDate = now.Date.AddDays(-10)
            },
            new()
            {
                Title = "Far future",
                Priority = "High",
                Status = 4,
                DueDate = now.Date.AddDays(30)
            }
        };

        var summary = WorkloadAnalyzer.Analyze(todos, now);

        Assert.Equal(4, summary.TotalTodos);
        Assert.Equal(3, summary.ActiveTodos);
        Assert.Equal(1, summary.CompletedTodos);
        Assert.Equal(1, summary.OverdueTodos);
        Assert.Equal(1, summary.DueThisWeek);
        Assert.Equal(2, summary.ByPriority["High"]);
        Assert.Equal(1, summary.ByStatus["Completed"]);
    }

    [Fact]
    public void FilterByQuery_MatchesTitleAndDescription()
    {
        var todos = new List<TodoItem>
        {
            new() { Title = "Payment gateway", Description = "Stripe checkout" },
            new() { Title = "Login page", Description = "OAuth flow" },
            new() { Title = "Docs", Description = "payment FAQ" }
        };

        var hits = WorkloadAnalyzer.FilterByQuery(todos, "payment");

        Assert.Equal(2, hits.Count);
        Assert.Contains(hits, t => t.Title == "Payment gateway");
        Assert.Contains(hits, t => t.Title == "Docs");
    }
}

public class McpServerOptionsTests
{
    [Fact]
    public void Validate_RequiresAuth0_WhenNoStaticToken()
    {
        var options = new McpServerOptions
        {
            ApiUrl = "http://localhost:5000",
            AccessToken = "",
            Auth0Domain = "",
            Auth0ClientId = ""
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("AUTH0_DOMAIN", ex.Message);
    }

    [Fact]
    public void Validate_AllowsStaticAccessTokenOverride()
    {
        var options = new McpServerOptions
        {
            ApiUrl = "http://localhost:5000",
            AccessToken = "test-token"
        };

        options.Validate();
    }

    [Fact]
    public void FromEnvironment_DefaultsApiUrl()
    {
        var previous = CaptureEnv();
        try
        {
            ClearAuthEnv();
            Environment.SetEnvironmentVariable("API_URL", null);
            Environment.SetEnvironmentVariable("API_ACCESS_TOKEN", "test-token");

            var options = McpServerOptions.FromEnvironment();

            Assert.Equal(McpServerOptions.DefaultApiUrl, options.ApiUrl);
            Assert.Equal("test-token", options.AccessToken);
            Assert.True(options.UseStaticAccessToken);
        }
        finally
        {
            RestoreEnv(previous);
        }
    }

    [Fact]
    public void Auth0Authority_NormalizesDomain()
    {
        var options = new McpServerOptions { Auth0Domain = "dev-example.us.auth0.com" };
        Assert.Equal("https://dev-example.us.auth0.com/", options.Auth0Authority);
    }

    private static Dictionary<string, string?> CaptureEnv() => new()
    {
        ["API_URL"] = Environment.GetEnvironmentVariable("API_URL"),
        ["API_ACCESS_TOKEN"] = Environment.GetEnvironmentVariable("API_ACCESS_TOKEN"),
        ["AUTH0_DOMAIN"] = Environment.GetEnvironmentVariable("AUTH0_DOMAIN"),
        ["AUTH0_AUTHORITY"] = Environment.GetEnvironmentVariable("AUTH0_AUTHORITY"),
        ["AUTH0_CLIENT_ID"] = Environment.GetEnvironmentVariable("AUTH0_CLIENT_ID"),
        ["AUTH0_AUDIENCE"] = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE"),
        ["AUTH0_SCOPE"] = Environment.GetEnvironmentVariable("AUTH0_SCOPE")
    };

    private static void ClearAuthEnv()
    {
        foreach (var key in CaptureEnv().Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    private static void RestoreEnv(Dictionary<string, string?> previous)
    {
        foreach (var (key, value) in previous)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

public class TokenCacheStoreTests
{
    [Fact]
    public void SaveLoadClear_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"taskmanager-mcp-test-{Guid.NewGuid():N}.json");
        try
        {
            var store = new TokenCacheStore(path);
            store.Save(new TokenCacheEntry
            {
                AccessToken = "a",
                RefreshToken = "r",
                ExpiresAtUtc = DateTimeOffset.Parse("2026-07-17T12:00:00Z")
            });

            var loaded = store.Load();
            Assert.NotNull(loaded);
            Assert.Equal("a", loaded!.AccessToken);
            Assert.Equal("r", loaded.RefreshToken);

            store.Clear();
            Assert.Null(store.Load());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
