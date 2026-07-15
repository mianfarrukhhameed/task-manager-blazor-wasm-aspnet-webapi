#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.ServiceLayer.Todos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fistix.TaskManager.ServiceLayer.Background;

public sealed class EmbeddingBackgroundService : BackgroundService
{
    private readonly IEmbeddingQueue _embeddingQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<EmbeddingBackgroundService> _logger;

    public EmbeddingBackgroundService(
        IEmbeddingQueue embeddingQueue,
        IServiceScopeFactory scopeFactory,
        AiConfiguration aiConfig,
        ILogger<EmbeddingBackgroundService> logger)
    {
        _embeddingQueue = embeddingQueue;
        _scopeFactory = scopeFactory;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_aiConfig.Features.EnableEmbeddings)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IEmbeddingProcessor>();
                await processor.BackfillMissingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Embedding backfill on startup failed");
            }
        }

        _logger.LogInformation("Embedding background service started");

        try
        {
            await foreach (var todoExternalId in _embeddingQueue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IEmbeddingProcessor>();
                    await processor.ProcessQueuedAsync(todoExternalId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background embedding failed for todo {TodoExternalId}", todoExternalId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down.
        }
    }
}
