using Fistix.TaskManager.ServiceLayer.Notifications;
using Fistix.TaskManager.ViewModel.Dtos;
using Fistix.TaskManager.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApi.Notifications;

public class SignalRClassificationNotifier : IClassificationNotifier
{
    private readonly IHubContext<ClassificationHub> _hubContext;

    public SignalRClassificationNotifier(IHubContext<ClassificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAsync(TaskClassificationDto classification, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(ClassificationHub.GetGroupName(classification.TodoExternalId))
            .SendAsync(ClassificationHub.ClassificationUpdatedMethod, classification, cancellationToken);
    }
}
