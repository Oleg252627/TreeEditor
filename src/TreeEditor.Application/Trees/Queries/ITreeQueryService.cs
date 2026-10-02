using TreeEditor.Application.Trees.Queries.Models;

namespace TreeEditor.Application.Trees.Queries;

public interface ITreeQueryService
{
    Task<IReadOnlyList<TreeNodeDto>> GetRootsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TreeNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);

    Task<TreeNodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}