using Fistix.TaskManager.ViewModel.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Notifications;

/// <summary>
/// No-op notifier for tests and non-SignalR hosts. Not registered by ServiceLayer DI by default.
/// </summary>
public sealed class NullClassificationNotifier : IClassificationNotifier
{
    public Task NotifyAsync(TaskClassificationDto classification, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
