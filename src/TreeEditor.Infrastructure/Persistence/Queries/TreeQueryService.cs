using Microsoft.EntityFrameworkCore;
using TreeEditor.Application.Trees.Queries;
using TreeEditor.Application.Trees.Queries.Models;
using TreeEditor.Domain.Entities;

namespace TreeEditor.Infrastructure.Persistence.Queries;

public sealed class TreeQueryService(
    TreeEditorDbContext dbContext)
    : ITreeQueryService
{
    public Task<IReadOnlyList<TreeNodeDto>> GetRootsAsync(CancellationToken cancellationToken = default) =>
        GetByParentIdAsync(null, cancellationToken);

    public Task<IReadOnlyList<TreeNodeDto>> GetChildrenAsync(Guid parentId,
        CancellationToken cancellationToken = default) =>
        GetByParentIdAsync(parentId, cancellationToken);

    public async Task<TreeNodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var node = await ProjectNodes(dbContext.TreeNodes.Where(node => node.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        if (node is null)
        {
            return null;
        }

        var ancestorIds = await dbContext.Database.SqlQuery<Guid>($"""
            WITH RECURSIVE ancestors AS (
                SELECT id, parent_id, 0 AS depth
                FROM tree_nodes
                WHERE id = {id}

                UNION ALL

                SELECT parent.id, parent.parent_id, child.depth + 1
                FROM tree_nodes AS parent
                INNER JOIN ancestors AS child ON parent.id = child.parent_id
            )
            SELECT id AS "Value"
            FROM ancestors
            WHERE depth > 0
            ORDER BY depth
            """).ToListAsync(cancellationToken);

        return node with { AncestorIds = ancestorIds };
    }

    private async Task<IReadOnlyList<TreeNodeDto>> GetByParentIdAsync(Guid? parentId,
        CancellationToken cancellationToken) =>
        await ProjectNodes(dbContext.TreeNodes
                .Where(node => node.ParentId == parentId)
                .OrderBy(node => node.Value))
            .ToListAsync(cancellationToken);

    private IQueryable<TreeNodeDto> ProjectNodes(IQueryable<TreeNode> nodes) =>
        nodes.AsNoTracking()
            .Select(node => new TreeNodeDto(
                node.Id,
                node.ParentId,
                node.Value,
                node.Version,
                dbContext.TreeNodes.Any(child => child.ParentId == node.Id)));
}