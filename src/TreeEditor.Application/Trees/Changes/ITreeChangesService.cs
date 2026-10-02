using TreeEditor.Application.Trees.Changes.Models;

namespace TreeEditor.Application.Trees.Changes;

public interface ITreeChangesService
{
    Task<ApplyTreeChangesResult> ApplyAsync(ApplyTreeChangesCommand command, CancellationToken cancellationToken = default);
}