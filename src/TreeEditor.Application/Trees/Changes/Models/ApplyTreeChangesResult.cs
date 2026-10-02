namespace TreeEditor.Application.Trees.Changes.Models;

public sealed record ApplyTreeChangesResult(IReadOnlyCollection<AppliedTreeNode> Nodes);

public sealed record AppliedTreeNode(Guid Id, uint Version);
