using Fistix.TaskManager.ViewModel.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.ServiceLayer.Notifications;

public interface IClassificationNotifier
{
    Task NotifyAsync(TaskClassificationDto classification, CancellationToken cancellationToken = default);
}
