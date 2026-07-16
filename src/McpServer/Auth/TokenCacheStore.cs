using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fistix.TaskManager.McpServer.Auth;

public sealed class TokenCacheEntry
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class TokenCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly object _gate = new();

    public TokenCacheStore(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultPath();
    }

    public string FilePath => _filePath;

    public TokenCacheEntry? Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<TokenCacheEntry>(json, JsonOptions);
        }
    }

    public void Save(TokenCacheEntry entry)
    {
        lock (_gate)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(entry, JsonOptions);
            File.WriteAllText(_filePath, json);

            TryRestrictPermissions(_filePath);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }

    public static string GetDefaultPath()
    {
        var root = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config");

        return Path.Combine(root, "taskmanager-mcp", "tokens.json");
    }

    private static void TryRestrictPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            // owner read/write only
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Best-effort on platforms that support Unix modes.
        }
    }
}
