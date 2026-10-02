namespace TreeEditor.Application.Trees.Changes.Models;

public sealed record AddedTreeNode(Guid Id, Guid ParentId, string Value);