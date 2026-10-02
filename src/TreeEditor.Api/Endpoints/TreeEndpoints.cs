using TreeEditor.Application.Trees.Changes;
using TreeEditor.Application.Trees.Changes.Models;
using TreeEditor.Application.Trees.Queries;
using TreeEditor.Application.Trees.Queries.Models;
using TreeEditor.Application.Trees.Reset;

namespace TreeEditor.Api.Endpoints;

public static class TreeEndpoints
{
    public static IEndpointRouteBuilder MapTreeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/tree")
            .WithTags("Tree")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/roots", GetRootsAsync)
            .WithSummary("Get root nodes")
            .WithDescription("Returns only the top-level nodes; children are loaded separately.")
            .Produces<IReadOnlyList<TreeNodeDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}/children", GetChildrenAsync)
            .WithSummary("Get direct children")
            .WithDescription("Returns the immediate children of a node without loading its entire subtree.")
            .Produces<IReadOnlyList<TreeNodeDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithSummary("Get a node for the local cache")
            .WithDescription("Returns one node with ancestor IDs ordered from its parent to the root.")
            .Produces<TreeNodeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/apply", ApplyAsync)
            .WithSummary("Apply cached tree changes")
            .WithDescription("Applies additions, value changes and subtree deletions in one transaction. " +
                             "Returns the new versions of the added and modified nodes.")
            .Produces<ApplyTreeChangesResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/reset", ResetAsync)
            .WithSummary("Restore the sample tree")
            .WithDescription("Replaces the database tree with the initial sample data.")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }

    private static async Task<IResult> GetRootsAsync(ITreeQueryService treeQueryService,
        CancellationToken cancellationToken)
    {
        var nodes = await treeQueryService.GetRootsAsync(cancellationToken);

        return Results.Ok(nodes);
    }

    private static async Task<IResult> GetChildrenAsync(Guid id, ITreeQueryService treeQueryService,
        CancellationToken cancellationToken)
    {
        var nodes = await treeQueryService.GetChildrenAsync(id, cancellationToken);

        return Results.Ok(nodes);
    }

    private static async Task<IResult> GetByIdAsync(Guid id, ITreeQueryService treeQueryService,
        CancellationToken cancellationToken)
    {
        var node = await treeQueryService.GetByIdAsync(id, cancellationToken);

        return node is null
            ? Results.NotFound()
            : Results.Ok(node);
    }

    private static async Task<IResult> ApplyAsync(ApplyTreeChangesCommand command,
        ITreeChangesService treeChangesService, CancellationToken cancellationToken)
    {
        var result = await treeChangesService.ApplyAsync(command, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> ResetAsync(ITreeDatabaseResetter treeDatabaseResetter,
        CancellationToken cancellationToken)
    {
        await treeDatabaseResetter.ResetAsync(cancellationToken);

        return Results.NoContent();
    }
}