using Fistix.TaskManager.ServiceLayer.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Fistix.TaskManager.ServiceLayer.Tests;

public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Registers no-op classification notifier for unit/integration tests that build ServiceLayer DI
    /// without SignalR/WebApi hosting.
    /// </summary>
    public static IServiceCollection AddNullClassificationNotifier(this IServiceCollection services)
    {
        services.AddSingleton<IClassificationNotifier, NullClassificationNotifier>();
        return services;
    }
}
