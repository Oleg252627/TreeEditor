using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreeEditor.Infrastructure.Persistence.Seed;

namespace TreeEditor.Infrastructure.Persistence;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TreeEditorDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        
        if (await dbContext.TreeNodes.AnyAsync(cancellationToken)) return;

        dbContext.TreeNodes.AddRange(InitialTreeData.Create());

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}