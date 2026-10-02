namespace TreeEditor.Application.Trees.Queries.Models;

public sealed record TreeNodeDto(
    Guid Id,
    Guid? ParentId,
    string Value,
    uint Version,
    bool HasChildren)
{
    public IReadOnlyList<Guid>? AncestorIds { get; init; }
}