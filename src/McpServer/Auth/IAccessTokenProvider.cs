namespace Fistix.TaskManager.McpServer.Auth;

public interface IAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
