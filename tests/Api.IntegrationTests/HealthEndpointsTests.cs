using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace Api.IntegrationTests;

[Collection("ApiDb")]
public sealed class HealthEndpointsTests(ApiDbFactory factory)
{
    [Fact]
    public async Task Health_WhenProcessIsRunning_ShouldReturnHealthyWithoutAuthentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task Ready_WhenDatabaseIsReachable_ShouldReturnHealthyWithoutAuthentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/ready");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task Ready_WhenDatabaseIsUnreachable_ShouldReturnServiceUnavailable()
    {
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SnackBuildersDb"] =
                        "Host=127.0.0.1;Port=1;Database=snackbuilders;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1"
                }));
        });
        using var client = host.CreateClient();

        var response = await client.GetAsync("/ready");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        body.Should().Be("Unhealthy");
    }
}
