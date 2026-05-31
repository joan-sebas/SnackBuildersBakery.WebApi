using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Infrastructure.IntegrationTests;

/// <summary>
/// Shared Postgres container for the integration test collection.
/// One container per test run; each test gets its own DbContext instance.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
#pragma warning disable CS0618 // PostgreSqlBuilder() parameterless ctor is deprecated in 4.x; pinned until DotNet.Testcontainers updates the image-first API.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();
#pragma warning restore CS0618

    public string ConnectionString => _container.GetConnectionString();

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations so the schema is ready before any test runs.
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
