using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.Core.Exceptions;
using Fistix.TaskManager.Core.SecurityModel;

namespace Fistix.TaskManager.Core.Tests;

public class TodoAccessGuardTests
{
    [Fact]
    public void EnsureCanAccess_AllowsAdmin_ForAnyTask()
    {
        var ownerId = Guid.NewGuid();
        var task = CreateTask(ownerId);
        var admin = CreateUser(isAdmin: true);

        var exception = Record.Exception(() => TodoAccessGuard.EnsureCanAccess(task, admin));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanAccess_AllowsOwner_ForOwnTask()
    {
        var owner = CreateUser(isAdmin: false);
        var task = CreateTask(owner.UserProfile.ExternalId);

        var exception = Record.Exception(() => TodoAccessGuard.EnsureCanAccess(task, owner));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanAccess_ThrowsForbidden_WhenNonOwnerAccessesTask()
    {
        var owner = CreateUser(isAdmin: false);
        var otherUser = CreateUser(isAdmin: false);
        var task = CreateTask(owner.UserProfile.ExternalId);

        Assert.Throws<ForbiddenAccessException>(() => TodoAccessGuard.EnsureCanAccess(task, otherUser));
    }

    [Fact]
    public void EnsureCanAccess_ThrowsForbidden_WhenUserProfileMissing()
    {
        var task = CreateTask(Guid.NewGuid());
        var userWithoutProfile = new StubCurrentUser
        {
            UserProfile = new UserProfile()
        };

        var exception = Assert.Throws<ForbiddenAccessException>(() =>
            TodoAccessGuard.EnsureCanAccess(task, userWithoutProfile));

        Assert.Contains("User profile not found", exception.Message);
    }

    [Fact]
    public void RequireCurrentUserId_ReturnsProfileExternalId_WhenProfileExists()
    {
        var user = CreateUser(isAdmin: false);

        var userId = TodoAccessGuard.RequireCurrentUserId(user);

        Assert.Equal(user.UserProfile.ExternalId, userId);
    }

    [Fact]
    public void RequireCurrentUserId_ThrowsForbidden_WhenProfileMissing()
    {
        var userWithoutProfile = new StubCurrentUser
        {
            UserProfile = new UserProfile()
        };

        Assert.Throws<ForbiddenAccessException>(() =>
            TodoAccessGuard.RequireCurrentUserId(userWithoutProfile));
    }

    private static TodoTask CreateTask(Guid createdByUserId)
    {
        return new TodoTask
        {
            Title = "Test task",
            Description = "Test description",
            CreatedByUserId = createdByUserId
        };
    }

    private static StubCurrentUser CreateUser(bool isAdmin)
    {
        var profile = new UserProfile
        {
            Name = isAdmin ? "admin" : "dev",
            EmailAddress = isAdmin ? "admin@test.com" : "dev@test.com",
            IsAdmin = isAdmin
        };
        profile.GenerateNewExternalId();

        return new StubCurrentUser
        {
            Email = profile.EmailAddress,
            HasAdminProfile = isAdmin,
            UserProfile = profile
        };
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public string Email { get; set; } = string.Empty;
        public bool HasAdminProfile { get; set; }
        public UserProfile UserProfile { get; set; } = new();
    }
}
