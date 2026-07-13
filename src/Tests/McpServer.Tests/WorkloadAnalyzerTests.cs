using Fistix.TaskManager.McpServer.Api;
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
    public void Validate_RequiresAccessToken()
    {
        var options = new McpServerOptions
        {
            ApiUrl = "https://localhost:5001",
            AccessToken = ""
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("API_ACCESS_TOKEN", ex.Message);
    }

    [Fact]
    public void FromEnvironment_DefaultsApiUrl()
    {
        var previousUrl = Environment.GetEnvironmentVariable("API_URL");
        var previousToken = Environment.GetEnvironmentVariable("API_ACCESS_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("API_URL", null);
            Environment.SetEnvironmentVariable("API_ACCESS_TOKEN", "test-token");

            var options = McpServerOptions.FromEnvironment();

            Assert.Equal(McpServerOptions.DefaultApiUrl, options.ApiUrl);
            Assert.Equal("test-token", options.AccessToken);
        }
        finally
        {
            Environment.SetEnvironmentVariable("API_URL", previousUrl);
            Environment.SetEnvironmentVariable("API_ACCESS_TOKEN", previousToken);
        }
    }
}
