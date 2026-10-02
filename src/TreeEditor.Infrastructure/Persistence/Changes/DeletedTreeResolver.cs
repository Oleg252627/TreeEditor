using Microsoft.EntityFrameworkCore;

namespace TreeEditor.Infrastructure.Persistence.Changes;

public sealed class DeletedTreeResolver(TreeEditorDbContext dbContext)
{
    public async Task<HashSet<Guid>> ResolveAsync(IReadOnlyCollection<Guid> rootIds,
        CancellationToken cancellationToken)
    {
        if (rootIds.Count == 0)
        {
            return [];
        }

        var ids = rootIds.ToArray();

        var deletedIds = await dbContext.Database
            .SqlQuery<Guid>($"""
                             WITH RECURSIVE deleted_tree AS
                             (
                                 SELECT id
                                 FROM tree_nodes
                                 WHERE id = ANY({ids})

                                 UNION ALL

                                 SELECT child.id
                                 FROM tree_nodes AS child
                                 INNER JOIN deleted_tree AS parent
                                     ON child.parent_id = parent.id
                             )
                             SELECT id AS "Value"
                             FROM deleted_tree
                             """)
            .ToListAsync(cancellationToken);

        return deletedIds.ToHashSet();
    }
}