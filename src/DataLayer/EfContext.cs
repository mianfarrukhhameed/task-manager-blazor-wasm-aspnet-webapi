using Fistix.TaskManager.Core.DomainModel.Aggregates;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Fistix.TaskManager.DataLayer
{
  public class EfContext: DbContext
  {
    public EfContext(DbContextOptions<EfContext> options) : base(options)
    {}

    public DbSet<TodoTask> TodoTasks { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<TodoAiMetadata> TodoAiMetadatas { get; set; }
    public DbSet<TodoEmbedding> TodoEmbeddings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.HasPostgresExtension("vector");

      TodoTaskModelConfig(modelBuilder);
      UserProfileModelConfig(modelBuilder);
      TodoAiMetadataModelConfig(modelBuilder);
      TodoEmbeddingModelConfig(modelBuilder);
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
          .HasDefaultValueSql("gen_random_uuid()")
          .IsRequired();
        entityModel.HasIndex(k => k.ExternalId)
          .IsUnique();
        entityModel.Property(p => p.CreatedByUserId)
          .IsRequired();
        entityModel.HasIndex(p => p.CreatedByUserId);
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

    private void TodoEmbeddingModelConfig(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<TodoEmbedding>(entityModel =>
      {
        entityModel.ToTable("TodoEmbeddings");
        entityModel.HasKey(k => k.Id);
        entityModel.Property(p => p.Embedding)
          .HasColumnType("vector(384)");
        entityModel.Property(p => p.EmbeddingModel)
          .HasMaxLength(100)
          .IsRequired();
        entityModel.HasIndex(e => new { e.TodoId, e.EmbeddingModel }).IsUnique();
        entityModel.HasOne(e => e.TodoTask)
          .WithMany()
          .HasForeignKey(e => e.TodoId)
          .OnDelete(DeleteBehavior.Cascade);
      });
    }
  }
}
