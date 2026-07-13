using Fistix.TaskManager.Core.Abstractions.Repositories;
using Fistix.TaskManager.Core.Config;
using Fistix.TaskManager.DataLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using System;

namespace Fistix.TaskManager.DataLayer
{
  public static class Startup
  {
    public static void AddDataLayer(this IServiceCollection services, MasterConfig masterConfig)
    {
      services.AddDbContext<EfContext>(options =>
        options.UseNpgsql(masterConfig.ConnectionString.MainDb, o => o.UseVector()));
      services.AddScoped<ITodoTaskRepository, TodoTaskRepository>();
      services.AddScoped<ITodoAiMetadataRepository, TodoAiMetadataRepository>();
      services.AddScoped<ITodoEmbeddingRepository, TodoEmbeddingRepository>();
      services.AddScoped<IUserProfileRepository, UserProfileRepository>();
      services.AddScoped<IRepositoryFactory, RepositoryFactory>();
    }

    public static void ApplyMigrations(IServiceProvider serviceProvider)
    {
      using var scope = serviceProvider.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<EfContext>();
      context.Database.Migrate();
    }
  }
}
