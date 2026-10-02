using Microsoft.Extensions.Logging;
using TreeEditor.Application.Trees.Changes;
using TreeEditor.Application.Trees.Changes.Models;

namespace TreeEditor.Infrastructure.Persistence.Changes.Decorators;

public sealed class TransactionTreeChangesService(
    ITreeChangesService inner,
    TreeEditorDbContext dbContext,
    ILogger<TransactionTreeChangesService> logger)
    : ITreeChangesService
{
    public async Task<ApplyTreeChangesResult> ApplyAsync(ApplyTreeChangesCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var result = await inner.ApplyAsync(command, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Tree changes applied: {AddedCount} added, {ModifiedCount} modified, {DeletedCount} deleted",
            command.Added.Count, command.Modified.Count, command.Deleted.Count);

        return result;
    }
}