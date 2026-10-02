using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace TreeEditor.IntegrationTests;

public sealed class TreeApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("tree_editor_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public string ConnectionString => _database.GetConnectionString();

    public IServiceProvider Services => _factory?.Services ??
                                        throw new InvalidOperationException("Fixture is not initialized.");

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        try
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    builder.UseSetting("ConnectionStrings:Database", ConnectionString);
                });

            Client = _factory.CreateClient();
        }
        catch
        {
            if (_factory is not null)
            {
                await _factory.DisposeAsync();
            }

            await _database.DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _database.DisposeAsync();
    }
}
