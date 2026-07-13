using System.Text.Json.Serialization;

namespace Fistix.TaskManager.McpServer.Api;

public sealed class TodoListResponse
{
    [JsonPropertyName("payload")]
    public List<TodoItem> Payload { get; set; } = [];
}

public sealed class TodoItem
{
    [JsonPropertyName("externalId")]
    public Guid ExternalId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

public sealed class CreateTodoRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }
}

public sealed class UpdateTodoRequest
{
    [JsonPropertyName("externalId")]
    public Guid ExternalId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "Medium";
}

public sealed class SemanticSearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;
}

public sealed class SemanticSearchResponse
{
    [JsonPropertyName("results")]
    public List<SemanticSearchHit> Results { get; set; } = [];

    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}

public sealed class SemanticSearchHit
{
    [JsonPropertyName("todoExternalId")]
    public Guid TodoExternalId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("similarity")]
    public double Similarity { get; set; }
}
