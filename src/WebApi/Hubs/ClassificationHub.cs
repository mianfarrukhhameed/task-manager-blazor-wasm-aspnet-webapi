using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.SecurityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApi.Hubs;

[Authorize]
public class ClassificationHub : Hub
{
    public const string HubPath = "/hubs/classification";
    public const string ClassificationUpdatedMethod = "ClassificationUpdated";

    private readonly ITodoTaskRepository _todoTaskRepository;
    private readonly ICurrentUserService _currentUserService;

    public ClassificationHub(ITodoTaskRepository todoTaskRepository, ICurrentUserService currentUserService)
    {
        _todoTaskRepository = todoTaskRepository;
        _currentUserService = currentUserService;
    }

    public async Task JoinTodo(Guid todoExternalId)
    {
        var todo = await _todoTaskRepository.Get(todoExternalId, Context.ConnectionAborted);
        TodoAccessGuard.EnsureCanAccess(todo, _currentUserService);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(todoExternalId));
    }

    public Task LeaveTodo(Guid todoExternalId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(todoExternalId));

    public static string GetGroupName(Guid todoExternalId) => $"todo-classification:{todoExternalId}";
}
