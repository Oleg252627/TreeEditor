using TreeEditor.Application.Common.Exceptions;
using TreeEditor.Application.Common.Validation;
using TreeEditor.Application.Trees.Changes.Models;
using TreeEditor.Domain.Entities;

namespace TreeEditor.Application.Trees.Changes.Validation;

public sealed class ApplyTreeChangesValidator : IValidator<ApplyTreeChangesCommand>
{
    public void Validate(ApplyTreeChangesCommand command)
    {
        if (command.Added is null || command.Modified is null || command.Deleted is null)
        {
            throw new ValidationException("Added, modified and deleted collections are required.");
        }

        var addedIds = new HashSet<Guid>();
        var modifiedIds = new HashSet<Guid>();
        var deletedIds = new HashSet<Guid>();

        ValidateAdded(command.Added, addedIds);
        ValidateModified(command.Modified, modifiedIds);
        ValidateDeleted(command.Deleted, deletedIds);

        ValidateConflicts(addedIds, modifiedIds, deletedIds);
    }

    private static void ValidateAdded(IReadOnlyCollection<AddedTreeNode> nodes, HashSet<Guid> ids)
    {
        foreach (var node in nodes)
        {
            if (node.Id == Guid.Empty)
            {
                throw new ValidationException("Added node ID cannot be empty.");
            }

            if (node.ParentId == Guid.Empty)
            {
                throw new ValidationException($"Parent ID for node '{node.Id}' cannot be empty.");
            }

            if (node.ParentId == node.Id)
            {
                throw new ValidationException($"Node '{node.Id}' cannot be its own parent.");
            }

            ValidateValue(node.Id, node.Value);

            if (!ids.Add(node.Id))
            {
                throw new ValidationException($"Added node '{node.Id}' is duplicated.");
            }
        }
    }

    private static void ValidateModified(IReadOnlyCollection<ModifiedTreeNode> nodes, HashSet<Guid> ids)
    {
        foreach (var node in nodes)
        {
            if (node.Id == Guid.Empty)
            {
                throw new ValidationException("Modified node ID cannot be empty.");
            }

            if (node.Version == 0)
            {
                throw new ValidationException($"Version for node '{node.Id}' is invalid.");
            }

            ValidateValue(node.Id, node.Value);

            if (!ids.Add(node.Id))
            {
                throw new ValidationException($"Modified node '{node.Id}' is duplicated.");
            }
        }
    }

    private static void ValidateDeleted(IReadOnlyCollection<DeletedTreeNode> nodes, HashSet<Guid> ids)
    {
        foreach (var node in nodes)
        {
            if (node.Id == Guid.Empty)
            {
                throw new ValidationException("Deleted node ID cannot be empty.");
            }

            if (node.Version == 0)
            {
                throw new ValidationException($"Version for node '{node.Id}' is invalid.");
            }

            if (!ids.Add(node.Id))
            {
                throw new ValidationException($"Deleted node '{node.Id}' is duplicated.");
            }
        }
    }

    private static void ValidateValue(Guid nodeId, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"Value for node '{nodeId}' cannot be empty.");
        }

        if (value.Trim().Length > TreeNode.MaxValueLength)
        {
            throw new ValidationException(
                $"Value for node '{nodeId}' cannot exceed {TreeNode.MaxValueLength} characters.");
        }
    }

    private static void ValidateConflicts(HashSet<Guid> addedIds, HashSet<Guid> modifiedIds, HashSet<Guid> deletedIds)
    {
        foreach (var id in addedIds)
        {
            if (modifiedIds.Contains(id) || deletedIds.Contains(id))
            {
                throw new ValidationException($"Added node '{id}' has conflicting changes.");
            }
        }

        foreach (var id in modifiedIds)
        {
            if (deletedIds.Contains(id))
            {
                throw new ValidationException($"Node '{id}' cannot be both modified and deleted.");
            }
        }
    }
}