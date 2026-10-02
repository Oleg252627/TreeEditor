using TreeEditor.Application.Common.Validation;
using TreeEditor.Application.Trees.Changes.Models;

namespace TreeEditor.Application.Trees.Changes.Decorators;

public sealed class ValidationTreeChangesService(
    ITreeChangesService inner,
    IValidator<ApplyTreeChangesCommand> validator) : ITreeChangesService
{
    public Task<ApplyTreeChangesResult> ApplyAsync(ApplyTreeChangesCommand command, CancellationToken cancellationToken = default)
    {
        validator.Validate(command);

        return inner.ApplyAsync(command, cancellationToken);
    }
}