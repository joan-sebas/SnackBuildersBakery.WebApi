using Api.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.IntegrationTests;

public sealed class AuthorizationTests(ProblemDetailsApiFactory factory) : IClassFixture<ProblemDetailsApiFactory>
{
    private const string ManagerApiKey = "manager-test-key";
    private const string PublicApiKey = "public-test-key";

    [Fact]
    public async Task ManagerEndpoint_WhenCalledByPublicPrincipal_ShouldReturnForbiddenProblemDetails()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, PublicApiKey);

        var response = await client.GetAsync("/manager-only");
        var problem = await ReadProblemAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.GetProperty("type").GetString().Should().Be("https://httpstatuses.com/403");
        problem.GetProperty("title").GetString().Should().Be("Forbidden");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ManagerEndpoint_WhenCalledByManagerPrincipal_ShouldReturnOk()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, ManagerApiKey);

        var response = await client.GetAsync("/manager-only");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicEndpoint_WhenCalledByPublicPrincipal_ShouldReturnOk()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, PublicApiKey);

        var response = await client.GetAsync("/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenCalledAnonymously_ShouldReturnUnauthorizedProblemDetails()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/manager-only");
        var problem = await ReadProblemAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.GetProperty("type").GetString().Should().Be("https://httpstatuses.com/401");
        problem.GetProperty("title").GetString().Should().Be("Unauthorized");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private HttpClient CreateClient()
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:ApiKeys:ManagerKey"] = ManagerApiKey,
                    ["Auth:ApiKeys:PublicKey"] = PublicApiKey,
                    ["Auth:Roles:Manager"] = "manager-test-role",
                    ["Auth:Roles:Public"] = "public-test-role"
                });
            });

            builder.Configure(app =>
            {
                app.UseExceptionHandler();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(MapEndpoints);
            });
        }).CreateClient();
    }

    private static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/manager-only", () => Results.Ok()).RequireManager();
        endpoints.MapGet("/public", () => Results.Ok()).RequirePublic();
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return document!.RootElement.Clone();
    }
}
