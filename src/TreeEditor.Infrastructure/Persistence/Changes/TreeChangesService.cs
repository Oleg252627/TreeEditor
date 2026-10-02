using Microsoft.EntityFrameworkCore;
using TreeEditor.Application.Common.Exceptions;
using TreeEditor.Application.Trees.Changes;
using TreeEditor.Application.Trees.Changes.Models;
using TreeEditor.Domain.Entities;

namespace TreeEditor.Infrastructure.Persistence.Changes;

public sealed class TreeChangesService( TreeEditorDbContext dbContext): ITreeChangesService
{
    public async Task<ApplyTreeChangesResult> ApplyAsync(ApplyTreeChangesCommand command,
        CancellationToken cancellationToken = default)
    {
        await ApplyDeletedAsync(command.Deleted,cancellationToken);

        var modified = await ApplyModifiedAsync(command.Modified,cancellationToken);

        var added = ApplyAdded(command.Added);

        await dbContext.SaveChangesAsync(cancellationToken);

        // xmin is store-generated, so the entities hold the new versions after saving.
        return new ApplyTreeChangesResult(modified
            .Concat(added)
            .Select(entity => new AppliedTreeNode(entity.Id, entity.Version))
            .ToList());
    }

    private async Task ApplyDeletedAsync(IReadOnlyCollection<DeletedTreeNode> nodes,
        CancellationToken cancellationToken)
    {
        if (nodes.Count == 0) return;

        var ids = nodes
            .Select(node => node.Id)
            .ToHashSet();

        var entities = await dbContext.TreeNodes
            .Where(node => ids.Contains(node.Id))
            .ToDictionaryAsync(
                node => node.Id,
                cancellationToken);

        foreach (var node in nodes)
        {
            if (!entities.TryGetValue(node.Id, out var entity))
            {
                throw new NotFoundException($"Tree node '{node.Id}' was not found.");
            }

            SetOriginalVersion(entity, node.Version);

            dbContext.TreeNodes.Remove(entity);
        }
    }

    private async Task<IReadOnlyList<TreeNode>> ApplyModifiedAsync(IReadOnlyCollection<ModifiedTreeNode> nodes,
        CancellationToken cancellationToken)
    {
        if (nodes.Count == 0) return [];

        var ids = nodes
            .Select(node => node.Id)
            .ToHashSet();

        var entities = await dbContext.TreeNodes
            .Where(node => ids.Contains(node.Id))
            .ToDictionaryAsync(node => node.Id, cancellationToken);

        foreach (var node in nodes)
        {
            if (!entities.TryGetValue(node.Id, out var entity))
            {
                throw new NotFoundException($"Tree node '{node.Id}' was not found.");
            }

            SetOriginalVersion(entity, node.Version);

            entity.SetValue(node.Value);
        }

        return entities.Values.ToList();
    }

    private List<TreeNode> ApplyAdded(IReadOnlyCollection<AddedTreeNode> nodes)
    {
        var orderedNodes = OrderAddedNodes(nodes);
        var entities = new List<TreeNode>(orderedNodes.Count);

        foreach (var node in orderedNodes)
        {
            var entity = new TreeNode(node.Id, node.ParentId, node.Value);

            dbContext.TreeNodes.Add(entity);
            entities.Add(entity);
        }

        return entities;
    }

    private static IReadOnlyList<AddedTreeNode> OrderAddedNodes(IReadOnlyCollection<AddedTreeNode> nodes)
    {
        var nodesById = nodes.ToDictionary(node => node.Id);
        var result = new List<AddedTreeNode>(nodes.Count);
        var visited = new HashSet<Guid>();
        var visiting = new HashSet<Guid>();

        foreach (var node in nodes)
        {
            AddNode(node);
        }

        return result;

        void AddNode(AddedTreeNode node)
        {
            if (visited.Contains(node.Id)) return;

            if (!visiting.Add(node.Id))
            {
                throw new NotFoundException("Added nodes contain a circular parent relationship.");
            }

            if (nodesById.TryGetValue(node.ParentId, out var parent))
            {
                AddNode(parent);
            }

            visiting.Remove(node.Id);
            visited.Add(node.Id);

            result.Add(node);
        }
    }

    private void SetOriginalVersion(TreeNode entity, uint version)
    {
        dbContext.Entry(entity)
            .Property(node => node.Version)
            .OriginalValue = version;
    }
}