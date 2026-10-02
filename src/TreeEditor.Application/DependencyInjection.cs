using Microsoft.Extensions.DependencyInjection;
using TreeEditor.Application.Common.Validation;
using TreeEditor.Application.Trees.Changes.Models;
using TreeEditor.Application.Trees.Changes.Validation;

namespace TreeEditor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<ApplyTreeChangesCommand>, ApplyTreeChangesValidator>();

        return services;
    }
}