using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Api.IntegrationTests;

/// <summary>
/// Shared test factory that starts a Postgres container, applies migrations once,
/// and provides a configured API host for endpoint integration tests.
/// </summary>
public sealed class ApiDbFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ManagerKey = "api-test-manager-key";
    public const string PublicKey = "api-test-public-key";

#pragma warning disable CS0618
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();
#pragma warning restore CS0618

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations before the app starts so the startup seed can run.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SnackBuildersDb"] = _container.GetConnectionString(),
                ["Auth:ApiKeys:ManagerKey"] = ManagerKey,
                ["Auth:ApiKeys:PublicKey"] = PublicKey,
                ["Auth:Roles:Manager"] = "manager",
                ["Auth:Roles:Public"] = "public",
                // Explicit scheduler values guard against content-root resolution misses in CI.
                ["Scheduler:AgingFactor"] = "1.5",
                ["Scheduler:OvensCount"] = "2",
                ["Scheduler:TraysPerOven"] = "4",
                ["Scheduler:Turnover"] = "00:00:30",
                ["Scheduler:TierWeights:Vip"] = "10",
                ["Scheduler:TierWeights:Delivery"] = "5",
                ["Scheduler:TierWeights:WalkIn"] = "1",
                ["Scheduler:BakeTimes:Cookie"] = "00:05:00",
                ["Scheduler:BakeTimes:Pastry"] = "00:10:00",
                ["Scheduler:BakeTimes:Bread"] = "00:20:00",
                ["PaymentGateway:CashFailureRate"] = "0.0",
                ["PaymentGateway:CardFailureRate"] = "0.0",
                ["PaymentGateway:SimulatedLatencyMs"] = "0"
            }));
    }
}

[CollectionDefinition("ApiDb")]
public sealed class ApiDbCollection : ICollectionFixture<ApiDbFactory> { }
