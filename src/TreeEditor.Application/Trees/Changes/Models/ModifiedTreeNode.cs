namespace TreeEditor.Application.Trees.Changes.Models;

public sealed record ModifiedTreeNode(Guid Id, string Value, uint Version);