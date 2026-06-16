using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.DataLayer.Repositories
{
  public class TodoTaskRepository : ITodoTaskRepository
  {
    private readonly EfContext _context;

    public TodoTaskRepository(EfContext context)
    {
      _context = context;
    }

    public async Task<bool> Create(TodoTask todoTask, CancellationToken cancellationToken)
    {
      _context.TodoTasks.Add(todoTask);
      var effectedRecords = await _context.SaveChangesAsync(cancellationToken);

      return effectedRecords > 0;
    }

    public async Task<bool> Delete(Guid externalId, CancellationToken cancellationToken)
    {
      var todoTask = await _context.TodoTasks.FirstOrDefaultAsync(t => t.ExternalId == externalId, cancellationToken);
      if (todoTask == null)
        throw new NotFoundException();
      
      _context.TodoTasks.Remove(todoTask);
      var effectedRecords = await _context.SaveChangesAsync(cancellationToken);

      return effectedRecords > 0;
    }

    public async Task<TodoTask> Get(Guid externalId, CancellationToken calcellationToken)
    {
      var entity = await _context.TodoTasks
        .Include(t => t.AiMetadata)
        .FirstOrDefaultAsync(t => t.ExternalId == externalId, calcellationToken);
      if (entity == null)
        throw new NotFoundException();

      return entity;
    }

    public async Task<List<TodoTask>> GetAll(CancellationToken cancellationToken)
    {
      return await _context.TodoTasks
        .Include(t => t.AiMetadata)
        .ToListAsync(cancellationToken);
    }

    public async Task<bool> Update(TodoTask todoTask, CancellationToken calcellationToken)
    {
      _context.TodoTasks.Update(todoTask);
      var effectedRecords = await _context.SaveChangesAsync();

      return effectedRecords > 0;
    }
  }
}
