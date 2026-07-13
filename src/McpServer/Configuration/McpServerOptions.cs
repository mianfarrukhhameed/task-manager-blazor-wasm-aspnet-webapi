namespace Fistix.TaskManager.McpServer.Configuration;

/// <summary>
/// Environment-based settings for the standalone MCP process.
/// WebApi's Ai:Features:EnableMcp flag is conceptual documentation only —
/// this server runs as a separate process and is not gated by that flag.
/// </summary>
public sealed class McpServerOptions
{
    public const string DefaultApiUrl = "https://localhost:5001";

    public string ApiUrl { get; init; } = DefaultApiUrl;
    public string AccessToken { get; init; } = string.Empty;

    public static McpServerOptions FromEnvironment()
    {
        var apiUrl = Environment.GetEnvironmentVariable("API_URL");
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            apiUrl = DefaultApiUrl;
        }

        return new McpServerOptions
        {
            ApiUrl = apiUrl.TrimEnd('/'),
            AccessToken = Environment.GetEnvironmentVariable("API_ACCESS_TOKEN")?.Trim() ?? string.Empty
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AccessToken))
        {
            throw new InvalidOperationException(
                "API_ACCESS_TOKEN environment variable is required. " +
                "Set it to a valid JWT bearer token for the Task Manager WebApi.");
        }

        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"API_URL must be an absolute http/https URL. Current value: '{ApiUrl}'.");
        }
    }
}
