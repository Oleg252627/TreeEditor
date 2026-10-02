using TreeEditor.Application.Common.Exceptions;
using TreeEditor.Application.Trees.Changes.Models;
using TreeEditor.Application.Trees.Changes.Validation;

namespace TreeEditor.UnitTests.Changes;

public sealed class ApplyTreeChangesValidatorTests
{
    private readonly ApplyTreeChangesValidator _validator = new();

    [Fact]
    public void Validate_AcceptsIndependentChangesAndNewChildOfNewNode()
    {
        var existingId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();

        var command = new ApplyTreeChangesCommand(
            [
                new AddedTreeNode(newParentId, existingId, "Parent"),
                new AddedTreeNode(Guid.NewGuid(), newParentId, "Child")
            ],
            [new ModifiedTreeNode(Guid.NewGuid(), "Updated", 1)],
            [new DeletedTreeNode(Guid.NewGuid(), 2)]);

        _validator.Validate(command);
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public void Validate_RejectsInvalidIdsVersionsAndConflictingChanges(ApplyTreeChangesCommand command)
    {
        Assert.Throws<ValidationException>(() => _validator.Validate(command));
    }

    public static TheoryData<ApplyTreeChangesCommand> InvalidCommands
    {
        get
        {
            var id = Guid.NewGuid();
            var parentId = Guid.NewGuid();

            return new TheoryData<ApplyTreeChangesCommand>
            {
                Command([new AddedTreeNode(Guid.Empty, parentId, "Node")]),
                Command([new AddedTreeNode(id, Guid.Empty, "Node")]),
                Command([new AddedTreeNode(id, parentId, "A"), new AddedTreeNode(id, parentId, "B")]),
                Command(modified: [new ModifiedTreeNode(Guid.Empty, "Node", 1)]),
                Command(modified: [new ModifiedTreeNode(id, "Node", 0)]),
                Command(modified: [new ModifiedTreeNode(id, "A", 1), new ModifiedTreeNode(id, "B", 1)]),
                Command(deleted: [new DeletedTreeNode(Guid.Empty, 1)]),
                Command(deleted: [new DeletedTreeNode(id, 0)]),
                Command(deleted: [new DeletedTreeNode(id, 1), new DeletedTreeNode(id, 2)]),
                Command([new AddedTreeNode(id, parentId, "Node")],
                    modified: [new ModifiedTreeNode(id, "Updated", 1)]),
                Command([new AddedTreeNode(id, parentId, "Node")],
                    deleted: [new DeletedTreeNode(id, 1)]),
                Command(modified: [new ModifiedTreeNode(id, "Updated", 1)],
                    deleted: [new DeletedTreeNode(id, 1)]),
                Command([new AddedTreeNode(id, id, "Node")]),
                Command([new AddedTreeNode(id, parentId, "   ")]),
                Command([new AddedTreeNode(id, parentId, new string('x', 501))]),
                Command(modified: [new ModifiedTreeNode(id, "", 1)]),
                Command(modified: [new ModifiedTreeNode(id, new string('x', 501), 1)]),
                new ApplyTreeChangesCommand(null!, [], [])
            };
        }
    }

    private static ApplyTreeChangesCommand Command(
        IReadOnlyCollection<AddedTreeNode>? added = null,
        IReadOnlyCollection<ModifiedTreeNode>? modified = null,
        IReadOnlyCollection<DeletedTreeNode>? deleted = null) =>
        new(added ?? [], modified ?? [], deleted ?? []);
}
