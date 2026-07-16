using Fistix.TaskManager.McpServer.Api;

namespace Fistix.TaskManager.McpServer.Services;

public static class WorkloadAnalyzer
{
    // Matches Fistix.TaskManager.ViewModel.Enums.TodoTaskStatus
    private const int StatusNotStarted = 1;
    private const int StatusInProgress = 2;
    private const int StatusCompleted = 3;
    private const int StatusPending = 4;

    public static WorkloadSummary Analyze(IReadOnlyList<TodoItem> todos, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var weekEnd = now.Date.AddDays(7);

        var byPriority = todos
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Priority) ? "Medium" : t.Priority!)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var byStatus = todos
            .GroupBy(StatusName)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var overdue = todos.Count(t =>
            t.DueDate.HasValue
            && t.DueDate.Value.Date < now.Date
            && t.Status != StatusCompleted);

        var dueThisWeek = todos.Count(t =>
            t.DueDate.HasValue
            && t.DueDate.Value.Date >= now.Date
            && t.DueDate.Value.Date < weekEnd
            && t.Status != StatusCompleted);

        var active = todos.Count(t => t.Status is StatusNotStarted or StatusInProgress or StatusPending);
        var completed = todos.Count(t => t.Status == StatusCompleted);

        return new WorkloadSummary
        {
            TotalTodos = todos.Count,
            ActiveTodos = active,
            CompletedTodos = completed,
            OverdueTodos = overdue,
            DueThisWeek = dueThisWeek,
            ByPriority = byPriority,
            ByStatus = byStatus,
            GeneratedAtUtc = now
        };
    }

    public static IReadOnlyList<TodoItem> FilterByQuery(IReadOnlyList<TodoItem> todos, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return todos;
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return todos
            .Where(t => terms.All(term =>
                Contains(t.Title, term)
                || Contains(t.Description, term)
                || Contains(t.Priority, term)
                || Contains(t.Category, term)))
            .ToList();
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static string StatusName(TodoItem todo) => todo.Status switch
    {
        StatusNotStarted => "NotStarted",
        StatusInProgress => "InProgress",
        StatusCompleted => "Completed",
        StatusPending => "Pending",
        _ => $"Unknown({todo.Status})"
    };
}

public sealed class WorkloadSummary
{
    public int TotalTodos { get; init; }
    public int ActiveTodos { get; init; }
    public int CompletedTodos { get; init; }
    public int OverdueTodos { get; init; }
    public int DueThisWeek { get; init; }
    public Dictionary<string, int> ByPriority { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> ByStatus { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime GeneratedAtUtc { get; init; }
}
