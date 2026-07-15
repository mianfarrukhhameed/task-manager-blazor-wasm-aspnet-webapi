#nullable enable

using Fistix.TaskManager.Core.DomainModel.Aggregates;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.Core.Abstractions.Repositories;

public interface IAiConversationRepository
{
    Task AddAsync(AiConversation conversation, CancellationToken cancellationToken);
}
