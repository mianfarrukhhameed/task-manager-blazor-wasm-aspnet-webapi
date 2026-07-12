using Fistix.TaskManager.ServiceLayer.Notifications;
using Fistix.TaskManager.ViewModel.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Fistix.TaskManager.ServiceLayer.Tests;

public class NullClassificationNotifierRegistrationTests
{
    [Fact]
    public async Task AddNullClassificationNotifier_resolves_and_completes()
    {
        var services = new ServiceCollection();
        services.AddNullClassificationNotifier();

        await using var provider = services.BuildServiceProvider();
        var notifier = provider.GetRequiredService<IClassificationNotifier>();

        Assert.IsType<NullClassificationNotifier>(notifier);

        await notifier.NotifyAsync(new TaskClassificationDto
        {
            TodoExternalId = Guid.NewGuid(),
            Status = "Completed"
        });
    }
}
