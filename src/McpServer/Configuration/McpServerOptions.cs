namespace Fistix.TaskManager.McpServer.Configuration;

/// <summary>
/// Environment-based settings for the standalone MCP process.
/// Prefer Auth0 Device Code + refresh token; <see cref="AccessToken"/> is an optional override for CI/tests.
/// </summary>
public sealed class McpServerOptions
{
    public const string DefaultApiUrl = "http://localhost:5000";
    public const string DefaultAudience = "https://api.taskmanager.com/";
    public const string DefaultScope = "openid profile email offline_access";

    public string ApiUrl { get; init; } = DefaultApiUrl;

    /// <summary>Optional static bearer JWT (CI/tests). When set, Device Code auth is skipped.</summary>
    public string AccessToken { get; init; } = string.Empty;

    public string Auth0Domain { get; init; } = string.Empty;
    public string Auth0ClientId { get; init; } = string.Empty;
    public string Auth0Audience { get; init; } = DefaultAudience;
    public string Auth0Scope { get; init; } = DefaultScope;

    public bool UseStaticAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

    public string Auth0Authority
    {
        get
        {
            var domain = Auth0Domain.Trim().TrimEnd('/');
            if (domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return domain.TrimEnd('/') + "/";
            }

            return $"https://{domain}/";
        }
    }

    public static McpServerOptions FromEnvironment()
    {
        var apiUrl = Environment.GetEnvironmentVariable("API_URL");
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            apiUrl = DefaultApiUrl;
        }

        var audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE");
        if (string.IsNullOrWhiteSpace(audience))
        {
            audience = DefaultAudience;
        }

        var scope = Environment.GetEnvironmentVariable("AUTH0_SCOPE");
        if (string.IsNullOrWhiteSpace(scope))
        {
            scope = DefaultScope;
        }

        var domain = Environment.GetEnvironmentVariable("AUTH0_DOMAIN")
            ?? Environment.GetEnvironmentVariable("AUTH0_AUTHORITY")
            ?? string.Empty;

        return new McpServerOptions
        {
            ApiUrl = apiUrl.TrimEnd('/'),
            AccessToken = Environment.GetEnvironmentVariable("API_ACCESS_TOKEN")?.Trim() ?? string.Empty,
            Auth0Domain = domain.Trim(),
            Auth0ClientId = Environment.GetEnvironmentVariable("AUTH0_CLIENT_ID")?.Trim() ?? string.Empty,
            Auth0Audience = audience.Trim(),
            Auth0Scope = scope.Trim()
        };
    }

    public void Validate()
    {
        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"API_URL must be an absolute http/https URL. Current value: '{ApiUrl}'.");
        }

        if (UseStaticAccessToken)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Auth0Domain))
        {
            throw new InvalidOperationException(
                "AUTH0_DOMAIN (or AUTH0_AUTHORITY) is required when API_ACCESS_TOKEN is not set. " +
                "Create an Auth0 Native app with Device Code + Refresh Token grants.");
        }

        if (string.IsNullOrWhiteSpace(Auth0ClientId))
        {
            throw new InvalidOperationException(
                "AUTH0_CLIENT_ID is required when API_ACCESS_TOKEN is not set.");
        }

        if (string.IsNullOrWhiteSpace(Auth0Audience))
        {
            throw new InvalidOperationException("AUTH0_AUDIENCE is required.");
        }
    }
}
