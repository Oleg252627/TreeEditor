using TreeEditor.Domain.Entities;

namespace TreeEditor.UnitTests.Domain;

public sealed class TreeNodeTests
{
    [Fact]
    public void Constructor_ShouldCreateNode()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var node = new TreeNode(id, parentId, "Backend");

        Assert.Equal(id, node.Id);
        Assert.Equal(parentId, node.ParentId);
        Assert.Equal("Backend", node.Value);
    }

    [Fact]
    public void SetValue_ShouldUpdateValue()
    {
        var node = new TreeNode(Guid.NewGuid(), null, "Old value");

        node.SetValue("New value");

        Assert.Equal("New value", node.Value);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNodeIsItsOwnParent()
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new TreeNode(id, id, "Node"));
    }

    [Fact]
    public void SetValue_ShouldThrow_WhenValueIsEmpty()
    {
        var node = new TreeNode(Guid.NewGuid(), null, "Node");

        Assert.Throws<ArgumentException>(() => node.SetValue(" "));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new TreeNode(Guid.Empty, null, "Node"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenValueIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new TreeNode(Guid.NewGuid(), null, "  "));
    }

    [Fact]
    public void SetValue_ShouldTrimValueWithoutChangingParent()
    {
        var parentId = Guid.NewGuid();
        var node = new TreeNode(Guid.NewGuid(), parentId, "  Original  ");

        Assert.Equal("Original", node.Value);

        node.SetValue("  Updated  ");

        Assert.Equal("Updated", node.Value);
        Assert.Equal(parentId, node.ParentId);
    }
}