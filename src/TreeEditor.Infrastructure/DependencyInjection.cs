using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TreeEditor.Application.Trees.Changes;
using TreeEditor.Application.Trees.Changes.Decorators;
using TreeEditor.Application.Trees.Queries;
using TreeEditor.Application.Trees.Reset;
using TreeEditor.Infrastructure.Persistence;
using TreeEditor.Infrastructure.Persistence.Changes;
using TreeEditor.Infrastructure.Persistence.Changes.Decorators;
using TreeEditor.Infrastructure.Persistence.Queries;
using TreeEditor.Infrastructure.Persistence.Reset;

namespace TreeEditor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TreeEditorDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Database") ??
                                   throw new InvalidOperationException("Connection string 'Database' was not found.");

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITreeQueryService, TreeQueryService>();

        services.AddScoped<ITreeChangesService, TreeChangesService>();
        services.Decorate<ITreeChangesService, DatabaseValidationTreeChangesService>();
        services.Decorate<ITreeChangesService, TransactionTreeChangesService>();
        services.Decorate<ITreeChangesService, ValidationTreeChangesService>();
        services.AddScoped<DeletedTreeResolver>();

        services.AddScoped<ITreeDatabaseResetter, TreeDatabaseResetter>();

        return services;
    }
}