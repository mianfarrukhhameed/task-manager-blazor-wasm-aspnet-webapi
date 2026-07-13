#nullable enable

using Fistix.TaskManager.Core.DomainModel.Aggregates;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.Core.Abstractions.Repositories;

public interface ISprintRepository
{
    Task<bool> Create(Sprint sprint, CancellationToken cancellationToken);
    Task<Sprint> Get(Guid externalId, CancellationToken cancellationToken);
    Task<List<Sprint>> GetByOwner(Guid ownerExternalId, CancellationToken cancellationToken);
}
