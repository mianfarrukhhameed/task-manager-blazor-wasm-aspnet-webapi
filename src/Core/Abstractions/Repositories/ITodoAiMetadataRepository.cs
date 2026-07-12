using Fistix.TaskManager.Core.DomainModel.Aggregates;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.Core.Abstractions.Repositories;

public interface ITodoAiMetadataRepository
{
    Task<TodoAiMetadata?> GetByTodoExternalIdAsync(Guid todoExternalId, CancellationToken cancellationToken);
    Task UpsertSummaryAsync(int todoId, string summary, string model, CancellationToken cancellationToken);
    Task MarkClassificationPendingAsync(int todoId, CancellationToken cancellationToken);
    Task UpsertClassificationAsync(
        int todoId,
        string priority,
        float confidence,
        string? reason,
        string model,
        CancellationToken cancellationToken);
    Task MarkClassificationFailedAsync(int todoId, CancellationToken cancellationToken);
    Task SetWasOverriddenAsync(int todoId, bool wasOverridden, CancellationToken cancellationToken);
}
