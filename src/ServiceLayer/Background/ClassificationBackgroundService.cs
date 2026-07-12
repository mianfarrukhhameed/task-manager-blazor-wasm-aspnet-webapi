using Fistix.TaskManager.AiLayer.Shared;
using Fistix.TaskManager.ServiceLayer.Todos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Background;

public class ClassificationBackgroundService : BackgroundService
{
    private readonly IClassificationQueue _classificationQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiConfiguration _aiConfig;
    private readonly ILogger<ClassificationBackgroundService> _logger;

    public ClassificationBackgroundService(
        IClassificationQueue classificationQueue,
        IServiceScopeFactory scopeFactory,
        AiConfiguration aiConfig,
        ILogger<ClassificationBackgroundService> logger)
    {
        _classificationQueue = classificationQueue;
        _scopeFactory = scopeFactory;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxDegree = Math.Max(1, _aiConfig.Features.Classification.MaxDegreeOfParallelism);
        using var gate = new SemaphoreSlim(maxDegree, maxDegree);

        _logger.LogInformation(
            "Classification background service started with MaxDegreeOfParallelism={MaxDegree}",
            maxDegree);

        try
        {
            await foreach (var todoExternalId in _classificationQueue.DequeueAllAsync(stoppingToken))
            {
                await gate.WaitAsync(stoppingToken);
                _ = ProcessJobAsync(todoExternalId, gate, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
        finally
        {
            // Wait until in-flight jobs release their slots.
            for (var i = 0; i < maxDegree; i++)
            {
                try
                {
                    await gate.WaitAsync(CancellationToken.None);
                }
                catch
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessJobAsync(
        Guid todoExternalId,
        SemaphoreSlim gate,
        CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IClassificationProcessor>();
            _logger.LogInformation("Dequeued classification job for todo {TodoExternalId}", todoExternalId);
            await processor.ProcessQueuedAsync(todoExternalId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Classification cancelled for todo {TodoExternalId}", todoExternalId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background classification failed for todo {TodoExternalId}", todoExternalId);
        }
        finally
        {
            gate.Release();
        }
    }
}
