using System.Text.Json;
using System.Text.Json.Serialization;
using Fistix.TaskManager.McpServer.Configuration;
using Microsoft.Extensions.Logging;

namespace Fistix.TaskManager.McpServer.Auth;

public sealed class Auth0DeviceCodeTokenService : IAccessTokenProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly McpServerOptions _options;
    private readonly TokenCacheStore _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<Auth0DeviceCodeTokenService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAtUtc;

    public Auth0DeviceCodeTokenService(
        McpServerOptions options,
        TokenCacheStore cache,
        HttpClient httpClient,
        ILogger<Auth0DeviceCodeTokenService> logger)
    {
        _options = options;
        _cache = cache;
        _httpClient = httpClient;
        _logger = logger;

        var cached = _cache.Load();
        if (cached is not null
            && !string.IsNullOrWhiteSpace(cached.AccessToken)
            && !string.IsNullOrWhiteSpace(cached.RefreshToken))
        {
            _accessToken = cached.AccessToken;
            _refreshToken = cached.RefreshToken;
            _expiresAtUtc = cached.ExpiresAtUtc;
        }
    }

    public async Task<string> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh
                && !string.IsNullOrWhiteSpace(_accessToken)
                && DateTimeOffset.UtcNow < _expiresAtUtc.AddMinutes(-1))
            {
                return _accessToken;
            }

            if (!string.IsNullOrWhiteSpace(_refreshToken))
            {
                try
                {
                    await RefreshAsync(cancellationToken).ConfigureAwait(false);
                    return _accessToken!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Refresh token failed; starting device code login.");
                    _refreshToken = null;
                    _accessToken = null;
                    _cache.Clear();
                }
            }

            await DeviceCodeLoginAsync(cancellationToken).ConfigureAwait(false);
            return _accessToken!;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _accessToken = null;
            _refreshToken = null;
            _expiresAtUtc = default;
            _cache.Clear();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.Auth0ClientId,
            ["refresh_token"] = _refreshToken!,
            ["audience"] = _options.Auth0Audience
        });

        using var response = await _httpClient
            .PostAsync(new Uri(new Uri(_options.Auth0Authority), "oauth/token"), content, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Auth0 refresh failed ({(int)response.StatusCode}): {body}");
        }

        ApplyTokenResponse(body);
        Persist();
        _logger.LogInformation("Access token refreshed via Auth0 refresh_token grant.");
    }

    private async Task DeviceCodeLoginAsync(CancellationToken cancellationToken)
    {
        using var deviceContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.Auth0ClientId,
            ["scope"] = _options.Auth0Scope,
            ["audience"] = _options.Auth0Audience
        });

        using var deviceResponse = await _httpClient
            .PostAsync(new Uri(new Uri(_options.Auth0Authority), "oauth/device/code"), deviceContent, cancellationToken)
            .ConfigureAwait(false);

        var deviceBody = await deviceResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!deviceResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Auth0 device code request failed ({(int)deviceResponse.StatusCode}): {deviceBody}");
        }

        var device = JsonSerializer.Deserialize<DeviceCodeResponse>(deviceBody, JsonOptions)
            ?? throw new InvalidOperationException("Auth0 device code response was empty.");

        var verificationUrl = string.IsNullOrWhiteSpace(device.VerificationUriComplete)
            ? $"{device.VerificationUri}  code: {device.UserCode}"
            : device.VerificationUriComplete;

        var message =
            $"Task Manager MCP: sign in required.{Environment.NewLine}" +
            $"Open: {verificationUrl}{Environment.NewLine}" +
            $"If prompted, enter code: {device.UserCode}";

        // stderr — Claude Desktop / MCP logs (stdout is JSON-RPC).
        Console.Error.WriteLine(message);
        _logger.LogWarning("{DeviceLoginMessage}", message);

        var interval = Math.Max(device.Interval, 1);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(device.ExpiresIn);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken).ConfigureAwait(false);

            using var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["device_code"] = device.DeviceCode,
                ["client_id"] = _options.Auth0ClientId
            });

            using var tokenResponse = await _httpClient
                .PostAsync(new Uri(new Uri(_options.Auth0Authority), "oauth/token"), tokenContent, cancellationToken)
                .ConfigureAwait(false);

            var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (tokenResponse.IsSuccessStatusCode)
            {
                ApplyTokenResponse(tokenBody);
                Persist();
                _logger.LogInformation("Device code login succeeded; tokens cached at {CachePath}.", _cache.FilePath);
                Console.Error.WriteLine("Task Manager MCP: login succeeded.");
                return;
            }

            var error = TryReadError(tokenBody);
            if (string.Equals(error, "authorization_pending", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error, "slow_down", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(error, "slow_down", StringComparison.OrdinalIgnoreCase))
                {
                    interval += 2;
                }

                continue;
            }

            if (string.Equals(error, "expired_token", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error, "access_denied", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Auth0 device login failed ({error}). Restart MCP and try again.");
            }

            throw new InvalidOperationException(
                $"Auth0 device token poll failed ({(int)tokenResponse.StatusCode}): {tokenBody}");
        }

        throw new InvalidOperationException("Auth0 device login timed out. Restart MCP and try again.");
    }

    private void ApplyTokenResponse(string json)
    {
        var token = JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Auth0 token response was empty.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Auth0 token response missing access_token.");
        }

        _accessToken = token.AccessToken;
        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            _refreshToken = token.RefreshToken;
        }

        if (string.IsNullOrWhiteSpace(_refreshToken))
        {
            throw new InvalidOperationException(
                "Auth0 did not return a refresh_token. Enable Refresh Token / offline_access on the Native app.");
        }

        var lifetime = token.ExpiresIn > 0 ? token.ExpiresIn : 3600;
        _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(lifetime);
    }

    private void Persist()
    {
        _cache.Save(new TokenCacheEntry
        {
            AccessToken = _accessToken!,
            RefreshToken = _refreshToken!,
            ExpiresAtUtc = _expiresAtUtc
        });
    }

    private static string? TryReadError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString();
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private sealed class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = string.Empty;

        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = string.Empty;

        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; set; } = string.Empty;

        [JsonPropertyName("verification_uri_complete")]
        public string? VerificationUriComplete { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("interval")]
        public int Interval { get; set; } = 5;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
