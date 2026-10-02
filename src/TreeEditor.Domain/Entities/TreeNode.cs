using TreeEditor.Domain.Common;

namespace TreeEditor.Domain.Entities;

public sealed class TreeNode : IHasVersion
{
    public const int MaxValueLength = 500;

    private TreeNode()
    {
    }

    public TreeNode(Guid id, Guid? parentId, string value)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Node ID cannot be empty.",nameof(id));
        }

        if (parentId == id)
        {
            throw new ArgumentException("A node cannot be its own parent.",nameof(parentId));
        }

        Id = id;
        ParentId = parentId;

        SetValue(value);
    }

    public Guid Id { get; private set; }

    public Guid? ParentId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public uint Version { get; private set; }

    public void SetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Node value cannot be empty.",nameof(value));
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > MaxValueLength)
        {
            throw new ArgumentException($"Node value cannot exceed {MaxValueLength} characters.", nameof(value));
        }

        Value = normalizedValue;
    }
}