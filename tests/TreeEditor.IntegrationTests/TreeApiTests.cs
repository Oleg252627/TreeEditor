using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TreeEditor.Application.Trees.Changes.Models;
using TreeEditor.Application.Trees.Queries.Models;
using TreeEditor.Infrastructure.Persistence;

namespace TreeEditor.IntegrationTests;

public sealed class TreeApiTests(TreeApiFixture fixture) : IClassFixture<TreeApiFixture>
{
    private static readonly Guid CompanyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid EngineeringId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid BackendId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid PlatformId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid ApiId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid SalesId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    [Fact]
    public void Application_UsesDisposableTestDatabase()
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TreeEditorDbContext>();

        var actual = new NpgsqlConnectionStringBuilder(dbContext.Database.GetConnectionString());
        var expected = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);

        Assert.Equal(expected.Host, actual.Host);
        Assert.Equal(expected.Port, actual.Port);
        Assert.Equal("tree_editor_tests", actual.Database);
    }

    [Fact]
    public async Task Requests_ReturnUniqueTraceIdsIncludingForMissingNodes()
    {
        using var first = await fixture.Client.GetAsync("/api/tree/roots");
        using var second = await fixture.Client.GetAsync($"/api/tree/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        var firstTraceId = Assert.Single(first.Headers.GetValues("X-Trace-Id"));
        var secondTraceId = Assert.Single(second.Headers.GetValues("X-Trace-Id"));
        Assert.Equal(32, firstTraceId.Length);
        Assert.Equal(32, secondTraceId.Length);
        Assert.NotEqual(firstTraceId, secondTraceId);
    }

    [Fact]
    public async Task Queries_LoadOnlyRequestedLevelAndReturnOrderedAncestorsForSingleNode()
    {
        await ResetAsync();

        var roots = await fixture.Client.GetFromJsonAsync<List<TreeNodeDto>>("/api/tree/roots");
        var children = await fixture.Client.GetFromJsonAsync<List<TreeNodeDto>>($"/api/tree/{CompanyId}/children");
        var backend = await GetNodeAsync(BackendId);

        Assert.Equal(CompanyId, Assert.Single(roots!).Id);
        Assert.Equal([EngineeringId, SalesId], children!.Select(node => node.Id));
        Assert.Equal(EngineeringId, backend.ParentId);
        Assert.Equal([EngineeringId, CompanyId], backend.AncestorIds);
        Assert.True(backend.HasChildren);
    }

    [Fact]
    public async Task Apply_UpdatesValueAndCreatesNestedChildrenRegardlessOfRequestOrder()
    {
        await ResetAsync();
        var backend = await GetNodeAsync(BackendId);
        var newParentId = Guid.NewGuid();
        var newChildId = Guid.NewGuid();

        using var response = await ApplyAsync(
            [
                new AddedTreeNode(newChildId, newParentId, "Nested child"),
                new AddedTreeNode(newParentId, BackendId, "New parent")
            ],
            [new ModifiedTreeNode(BackendId, "Renamed backend", backend.Version)]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ApplyTreeChangesResult>())!;
        var renamedBackend = await GetNodeAsync(BackendId);
        var newChild = await GetNodeAsync(newChildId);
        Assert.Equal("Renamed backend", renamedBackend.Value);
        Assert.Equal(newParentId, newChild.ParentId);
        Assert.NotEqual(backend.Version, renamedBackend.Version);
        Assert.Equal(
            new[] { (BackendId, renamedBackend.Version), (newChildId, newChild.Version) }.ToHashSet(),
            result.Nodes.Where(node => node.Id != newParentId).Select(node => (node.Id, node.Version)).ToHashSet());
        Assert.Contains(result.Nodes, node => node.Id == newParentId);
        Assert.Contains(newParentId, (await fixture.Client.GetFromJsonAsync<List<TreeNodeDto>>(
            $"/api/tree/{BackendId}/children"))!.Select(node => node.Id));
    }

    [Fact]
    public async Task Apply_DeletesEntireSubtreeIncludingNodesNeverLoadedByClient()
    {
        await ResetAsync();
        var backend = await GetNodeAsync(BackendId);

        using var response = await ApplyAsync(deleted:
            [new DeletedTreeNode(BackendId, backend.Version)]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.GetAsync($"/api/tree/{ApiId}")).StatusCode);
        Assert.DoesNotContain((await fixture.Client.GetFromJsonAsync<List<TreeNodeDto>>(
            $"/api/tree/{EngineeringId}/children"))!, node => node.Id == BackendId);
        Assert.Equal(SalesId, (await GetNodeAsync(SalesId)).Id);
    }

    [Fact]
    public async Task Apply_RejectsStaleVersionWithoutSavingOtherChanges()
    {
        await ResetAsync();
        var backend = await GetNodeAsync(BackendId);
        var platform = await GetNodeAsync(PlatformId);
        var originalValue = platform.Value;

        using (var first = await ApplyAsync(modified:
                   [new ModifiedTreeNode(BackendId, "First change", backend.Version)]))
        {
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        }

        using var response = await ApplyAsync(modified:
        [
            new ModifiedTreeNode(BackendId, "Stale change", backend.Version),
            new ModifiedTreeNode(PlatformId, "Should roll back", platform.Version)
        ]);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Trace-Id")),
            problem.GetProperty("traceId").GetString());
        Assert.Equal("First change", (await GetNodeAsync(BackendId)).Value);
        Assert.Equal(originalValue, (await GetNodeAsync(PlatformId)).Value);
    }

    [Fact]
    public async Task Apply_RejectsChildOfDeletedSubtreeAndLeavesDatabaseUnchanged()
    {
        await ResetAsync();
        var backend = await GetNodeAsync(BackendId);
        var newId = Guid.NewGuid();

        using var response = await ApplyAsync(
            added: [new AddedTreeNode(newId, PlatformId, "Invalid child")],
            deleted: [new DeletedTreeNode(BackendId, backend.Version)]);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(BackendId, (await GetNodeAsync(BackendId)).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.GetAsync($"/api/tree/{newId}")).StatusCode);
    }

    [Fact]
    public async Task Reset_RestoresInitialTreeAfterEditsAdditionsAndDeletions()
    {
        await ResetAsync();
        var backend = await GetNodeAsync(BackendId);
        var sales = await GetNodeAsync(SalesId);
        var newId = Guid.NewGuid();

        using (var apply = await ApplyAsync(
                   added: [new AddedTreeNode(newId, EngineeringId, "Temporary")],
                   modified: [new ModifiedTreeNode(BackendId, "Changed", backend.Version)],
                   deleted: [new DeletedTreeNode(SalesId, sales.Version)]))
        {
            Assert.Equal(HttpStatusCode.OK, apply.StatusCode);
        }

        await ResetAsync();

        Assert.Equal("Backend", (await GetNodeAsync(BackendId)).Value);
        Assert.Equal(SalesId, (await GetNodeAsync(SalesId)).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client.GetAsync($"/api/tree/{newId}")).StatusCode);
    }

    [Fact]
    public async Task Apply_ReturnsClientErrorsForInvalidValuesMissingCollectionsAndExistingIds()
    {
        await ResetAsync();

        using (var tooLong = await ApplyAsync(added: [new AddedTreeNode(Guid.NewGuid(), CompanyId, new string('x', 501))]))
        {
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        }

        using (var empty = await ApplyAsync(added: [new AddedTreeNode(Guid.NewGuid(), CompanyId, "  ")]))
        {
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        }

        using (var missingCollections = await fixture.Client.PostAsync("/api/tree/apply",
                   new StringContent("{}", System.Text.Encoding.UTF8, "application/json")))
        {
            Assert.Equal(HttpStatusCode.BadRequest, missingCollections.StatusCode);
        }

        using (var existingId = await ApplyAsync(added: [new AddedTreeNode(BackendId, CompanyId, "Duplicate")]))
        {
            Assert.Equal(HttpStatusCode.Conflict, existingId.StatusCode);
        }

        Assert.Equal("Backend", (await GetNodeAsync(BackendId)).Value);
    }

    private async Task ResetAsync()
    {
        using var response = await fixture.Client.PostAsync("/api/tree/reset", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<TreeNodeDto> GetNodeAsync(Guid id) =>
        (await fixture.Client.GetFromJsonAsync<TreeNodeDto>($"/api/tree/{id}"))!;

    private Task<HttpResponseMessage> ApplyAsync(
        IReadOnlyCollection<AddedTreeNode>? added = null,
        IReadOnlyCollection<ModifiedTreeNode>? modified = null,
        IReadOnlyCollection<DeletedTreeNode>? deleted = null) =>
        fixture.Client.PostAsJsonAsync("/api/tree/apply",
            new ApplyTreeChangesCommand(added ?? [], modified ?? [], deleted ?? []));
}
