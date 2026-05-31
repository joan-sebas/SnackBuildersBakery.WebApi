using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Used exclusively by <c>dotnet ef</c> at design time. Never instantiated at runtime.
/// Reads the connection string from the <c>ConnectionStrings__SnackBuildersDb</c> environment
/// variable; falls back to the local dev default from <c>.env.example</c>.
/// </summary>
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__SnackBuildersDb")
            ?? "Host=localhost;Port=5432;Database=snack_builders;Username=snack_builders;Password=change_me";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new AppDbContext(options);
    }
}
