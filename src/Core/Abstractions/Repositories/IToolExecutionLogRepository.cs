#nullable enable

using Fistix.TaskManager.Core.DomainModel.Aggregates;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.Core.Abstractions.Repositories;

public interface IToolExecutionLogRepository
{
    Task AddAsync(ToolExecutionLog log, CancellationToken cancellationToken);
}
