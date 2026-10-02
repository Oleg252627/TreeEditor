using Microsoft.EntityFrameworkCore;
using TreeEditor.Application.Common.Exceptions;
using TreeEditor.Application.Trees.Changes;
using TreeEditor.Application.Trees.Changes.Models;

namespace TreeEditor.Infrastructure.Persistence.Changes.Decorators;

public sealed class DatabaseValidationTreeChangesService(
    ITreeChangesService inner,
    TreeEditorDbContext dbContext,
    DeletedTreeResolver deletedTreeResolver)
    : ITreeChangesService
{
    public async Task<ApplyTreeChangesResult> ApplyAsync(ApplyTreeChangesCommand command, CancellationToken cancellationToken = default)
    {
        var deletedTreeIds = await GetDeletedTreeIdsAsync(command.Deleted, cancellationToken);

        ValidateModifiedNodes(command.Modified, deletedTreeIds);

        await ValidateAddedParentsAsync(command.Added, deletedTreeIds, cancellationToken);

        var normalizedDeleted = await NormalizeDeletedAsync(command.Deleted, deletedTreeIds, cancellationToken);

        var normalizedCommand = command with
        {
            Deleted = normalizedDeleted
        };

        return await inner.ApplyAsync(normalizedCommand, cancellationToken);
    }

    private async Task<HashSet<Guid>> GetDeletedTreeIdsAsync(IReadOnlyCollection<DeletedTreeNode> deleted,
        CancellationToken cancellationToken)
    {
        if (deleted.Count == 0)
        {
            return [];
        }

        var rootIds = new List<Guid>(deleted.Count);

        foreach (var node in deleted)
        {
            rootIds.Add(node.Id);
        }

        return await deletedTreeResolver.ResolveAsync(rootIds, cancellationToken);
    }

    private static void ValidateModifiedNodes(IReadOnlyCollection<ModifiedTreeNode> modified,
        HashSet<Guid> deletedTreeIds)
    {
        foreach (var node in modified)
        {
            if (deletedTreeIds.Contains(node.Id))
            {
                throw new ConflictException(
                    $"Node '{node.Id}' cannot be modified because it is being deleted.");
            }
        }
    }

    private async Task ValidateAddedParentsAsync(IReadOnlyCollection<AddedTreeNode> added, HashSet<Guid> deletedTreeIds,
        CancellationToken cancellationToken)
    {
        if (added.Count == 0)
        {
            return;
        }

        var addedIds = new HashSet<Guid>();

        foreach (var node in added)
        {
            addedIds.Add(node.Id);
        }

        var existingAddedIds = await dbContext.TreeNodes
            .AsNoTracking()
            .Where(node => addedIds.Contains(node.Id))
            .Select(node => node.Id)
            .ToListAsync(cancellationToken);

        if (existingAddedIds.Count > 0)
        {
            throw new ConflictException($"Node '{existingAddedIds[0]}' already exists.");
        }

        var existingParentIds = new HashSet<Guid>();

        foreach (var node in added)
        {
            if (addedIds.Contains(node.ParentId))
            {
                continue;
            }

            if (deletedTreeIds.Contains(node.ParentId))
            {
                throw new ConflictException(
                    $"Cannot add a child to node '{node.ParentId}' because it is being deleted.");
            }

            existingParentIds.Add(node.ParentId);
        }

        if (existingParentIds.Count == 0)
        {
            return;
        }

        var foundParentIds = await dbContext.TreeNodes
            .AsNoTracking()
            .Where(node => existingParentIds.Contains(node.Id))
            .Select(node => node.Id)
            .ToListAsync(cancellationToken);

        var foundParentIdsSet = foundParentIds.ToHashSet();

        foreach (var parentId in existingParentIds)
        {
            if (!foundParentIdsSet.Contains(parentId))
            {
                throw new NotFoundException($"Parent node '{parentId}' does not exist.");
            }
        }
    }

    private async Task<IReadOnlyCollection<DeletedTreeNode>> NormalizeDeletedAsync(
        IReadOnlyCollection<DeletedTreeNode> deleted, HashSet<Guid> deletedTreeIds, CancellationToken cancellationToken)
    {
        if (deleted.Count <= 1)
        {
            return deleted;
        }

        var explicitlyDeletedIds = new HashSet<Guid>();

        foreach (var node in deleted)
        {
            explicitlyDeletedIds.Add(node.Id);
        }

        var parents = await dbContext.TreeNodes
            .AsNoTracking()
            .Where(node => deletedTreeIds.Contains(node.Id))
            .Select(node => new
            {
                node.Id,
                node.ParentId
            })
            .ToDictionaryAsync(
                node => node.Id,
                node => node.ParentId,
                cancellationToken);

        var result = new List<DeletedTreeNode>();

        foreach (var node in deleted)
        {
            if (HasDeletedAncestor(node.Id, explicitlyDeletedIds, parents))
            {
                continue;
            }

            result.Add(node);
        }

        return result;
    }

    private static bool HasDeletedAncestor(Guid nodeId, HashSet<Guid> explicitlyDeletedIds,
        IReadOnlyDictionary<Guid, Guid?> parents)
    {
        if (!parents.TryGetValue(nodeId, out var parentId))
        {
            return false;
        }

        while (parentId.HasValue)
        {
            if (explicitlyDeletedIds.Contains(parentId.Value))
            {
                return true;
            }

            if (!parents.TryGetValue(parentId.Value, out parentId))
            {
                return false;
            }
        }

        return false;
    }
}