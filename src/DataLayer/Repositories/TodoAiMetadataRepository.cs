using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.DataLayer.Repositories;

public class TodoAiMetadataRepository : ITodoAiMetadataRepository
{
    private readonly EfContext _context;

    public TodoAiMetadataRepository(EfContext context)
    {
        _context = context;
    }

    public async Task<TodoAiMetadata?> GetByTodoExternalIdAsync(Guid todoExternalId, CancellationToken cancellationToken)
    {
        var todoId = await _context.TodoTasks
            .Where(t => t.ExternalId == todoExternalId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (todoId is null)
        {
            return null;
        }

        return await _context.TodoAiMetadatas
            .FirstOrDefaultAsync(m => m.TodoId == todoId, cancellationToken);
    }

    public async Task UpsertSummaryAsync(int todoId, string summary, string model, CancellationToken cancellationToken)
    {
        var metadata = await _context.TodoAiMetadatas
            .FirstOrDefaultAsync(m => m.TodoId == todoId, cancellationToken);

        if (metadata is null)
        {
            metadata = new TodoAiMetadata
            {
                TodoId = todoId,
                CreatedAt = DateTime.UtcNow
            };
            _context.TodoAiMetadatas.Add(metadata);
        }

        metadata.AiSummary = summary;
        metadata.AiSummaryModel = model;
        metadata.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
