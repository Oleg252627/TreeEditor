using Microsoft.EntityFrameworkCore;
using TreeEditor.Domain.Common;
using TreeEditor.Domain.Entities;

namespace TreeEditor.Infrastructure.Persistence;

public sealed class TreeEditorDbContext(DbContextOptions<TreeEditorDbContext> options) : DbContext(options)
{
    public DbSet<TreeNode> TreeNodes => Set<TreeNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TreeEditorDbContext).Assembly);
        
        ConfigureConcurrency(modelBuilder);
    }
    
    private static void ConfigureConcurrency(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IHasVersion).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IHasVersion.Version))
                .HasColumnName("xmin")
                .IsRowVersion();
        }
    }
}