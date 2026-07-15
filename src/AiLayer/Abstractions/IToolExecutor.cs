#nullable enable

using Fistix.TaskManager.AiLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.AiLayer.Abstractions;

/// <summary>
/// Executes user-approved AI tool calls via authorized application commands.
/// </summary>
public interface IToolExecutor
{
    Task<IReadOnlyList<ToolExecutionOutcome>> ExecuteAsync(
        IReadOnlyList<ProposedToolCall> calls,
        CancellationToken cancellationToken = default);
}
