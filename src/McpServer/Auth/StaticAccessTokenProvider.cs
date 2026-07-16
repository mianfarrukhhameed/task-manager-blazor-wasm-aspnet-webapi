namespace Fistix.TaskManager.McpServer.Auth;

/// <summary>Uses a fixed JWT from <c>API_ACCESS_TOKEN</c> (CI / emergency override).</summary>
public sealed class StaticAccessTokenProvider : IAccessTokenProvider
{
    private readonly string _accessToken;

    public StaticAccessTokenProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public Task<string> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accessToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
