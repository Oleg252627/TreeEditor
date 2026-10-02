using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TreeEditor.Application.Trees.Reset;
using TreeEditor.Infrastructure.Persistence.Seed;

namespace TreeEditor.Infrastructure.Persistence.Reset;

public class TreeDatabaseResetter(TreeEditorDbContext dbContext, ILogger<TreeDatabaseResetter> logger) : ITreeDatabaseResetter
{
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.TreeNodes.ExecuteDeleteAsync(cancellationToken);

        var initialNodes = InitialTreeData.Create();
        dbContext.TreeNodes.AddRange(initialNodes);

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Tree reset to initial sample data with {NodeCount} nodes", initialNodes.Count);
    }
}