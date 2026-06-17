using System;
using Fistix.TaskManager.Core.Abstractions.Services;
using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Fistix.TaskManager.Core.Exceptions;

namespace Fistix.TaskManager.Core.SecurityModel
{
  public static class TodoAccessGuard
  {
    /// <summary>Seed admin user ExternalId — used for orphan task backfill in migrations.</summary>
    public static readonly Guid DefaultAdminUserExternalId =
      Guid.Parse("1efb2983-09be-47a5-ac2c-bff124d542ec");

    public static void EnsureCanAccess(TodoTask task, ICurrentUserService currentUser)
    {
      if (currentUser.HasAdminProfile)
        return;

      if (currentUser.UserProfile == null || currentUser.UserProfile.ExternalId == Guid.Empty)
        throw new ForbiddenAccessException("User profile not found.");

      if (task.CreatedByUserId != currentUser.UserProfile.ExternalId)
        throw new ForbiddenAccessException();
    }

    public static Guid RequireCurrentUserId(ICurrentUserService currentUser)
    {
      if (currentUser.UserProfile == null || currentUser.UserProfile.ExternalId == Guid.Empty)
        throw new ForbiddenAccessException("User profile not found.");

      return currentUser.UserProfile.ExternalId;
    }
  }
}
