using Fistix.TaskManager.ViewModel.Dtos;
using Fistix.TaskManager.WebApp.Hubs;
using Fistix.TaskManager.WebApp.Models.Config;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;

namespace Fistix.TaskManager.WebApp.Services;

public class ClassificationHubService : IAsyncDisposable
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ApiConfig _apiConfig;
    private readonly ILogger<ClassificationHubService> _logger;
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private Guid? _joinedTodoId;

    public ClassificationHubService(
        IAccessTokenProvider accessTokenProvider,
        ApiConfig apiConfig,
        ILogger<ClassificationHubService> logger)
    {
        _accessTokenProvider = accessTokenProvider;
        _apiConfig = apiConfig;
        _logger = logger;
    }

    public event Action<TaskClassificationDto>? ClassificationUpdated;

    public async Task SubscribeToTodoAsync(Guid todoExternalId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (_joinedTodoId.HasValue && _joinedTodoId.Value != todoExternalId)
        {
            await LeaveTodoAsync(_joinedTodoId.Value, cancellationToken);
        }

        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinTodo", todoExternalId, cancellationToken);
            _joinedTodoId = todoExternalId;
        }
    }

    public async Task LeaveTodoAsync(Guid todoExternalId, CancellationToken cancellationToken = default)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveTodo", todoExternalId, cancellationToken);
        }

        if (_joinedTodoId == todoExternalId)
        {
            _joinedTodoId = null;
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            return;
        }

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { State: HubConnectionState.Connected })
            {
                return;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            var hubUrl = new Uri(new Uri(_apiConfig.Url), ClassificationHubClient.HubPath);

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var tokenResult = await _accessTokenProvider.RequestAccessToken();
                        return tokenResult.TryGetToken(out var token) ? token.Value : null;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<TaskClassificationDto>(ClassificationHubClient.ClassificationUpdatedMethod, dto =>
            {
                ClassificationUpdated?.Invoke(dto);
            });

            await _connection.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to classification hub");
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_joinedTodoId.HasValue && _connection is { State: HubConnectionState.Connected })
        {
            try
            {
                await _connection.InvokeAsync("LeaveTodo", _joinedTodoId.Value);
            }
            catch
            {
                // Best effort on dispose.
            }
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectLock.Dispose();
    }
}
