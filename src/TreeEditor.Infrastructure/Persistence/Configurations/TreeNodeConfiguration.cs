using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TreeEditor.Domain.Entities;

namespace TreeEditor.Infrastructure.Persistence.Configurations;

public sealed class TreeNodeConfiguration : IEntityTypeConfiguration<TreeNode>
{
    public void Configure(EntityTypeBuilder<TreeNode> builder)
    {
        builder.ToTable("tree_nodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ParentId).HasColumnName("parent_id");

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasMaxLength(TreeNode.MaxValueLength)
            .IsRequired();

        builder.HasOne<TreeNode>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ParentId);
    }
}