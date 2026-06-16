using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fistix.TaskManager.DataLayer
{
  public class EfContext: DbContext
  {
    public EfContext(DbContextOptions<EfContext> options) : base(options)
    {}

    public DbSet<TodoTask> TodoTasks { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<TodoAiMetadata> TodoAiMetadatas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      TodoTaskModelConfig(modelBuilder);
      UserProfileModelConfig(modelBuilder);
      TodoAiMetadataModelConfig(modelBuilder);
    }

    private void TodoTaskModelConfig(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<TodoTask>(entityModel =>
      {
        entityModel.ToTable("TodoTask");
        entityModel.HasKey(k => k.Id);
        entityModel.Property(p => p.Id)
          .ValueGeneratedOnAdd();
        entityModel.Property(p => p.ExternalId)
          .HasDefaultValueSql("NEWSEQUENTIALID()")
          .IsRequired();
        entityModel.HasIndex(k => k.ExternalId)
          .IsUnique();
      });
    }
    private void UserProfileModelConfig(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<UserProfile>(entityModel =>
      {
        entityModel.ToTable("UserProfile");
        entityModel.HasKey(k => k.Id);
      });
    }
    private void TodoAiMetadataModelConfig(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<TodoAiMetadata>(entityModel =>
      {
        entityModel.ToTable("TodoAiMetadata");
        entityModel.HasKey(k => k.Id);
        entityModel.HasIndex(k => k.TodoId).IsUnique();
        
        entityModel.HasOne(m => m.TodoTask)
          .WithOne(t => t.AiMetadata)
          .HasForeignKey<TodoAiMetadata>(m => m.TodoId)
          .OnDelete(DeleteBehavior.Cascade);
      });
    }
  }
}
