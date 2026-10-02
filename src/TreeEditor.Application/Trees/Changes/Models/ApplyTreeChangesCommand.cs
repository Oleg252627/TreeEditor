namespace TreeEditor.Application.Trees.Changes.Models;

public sealed record ApplyTreeChangesCommand(
    IReadOnlyCollection<AddedTreeNode> Added,
    IReadOnlyCollection<ModifiedTreeNode> Modified,
    IReadOnlyCollection<DeletedTreeNode> Deleted);