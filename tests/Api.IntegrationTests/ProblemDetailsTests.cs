using Domain;
using FluentAssertions;
using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.IntegrationTests;

public sealed class ProblemDetailsTests(ProblemDetailsApiFactory factory) : IClassFixture<ProblemDetailsApiFactory>
{
    [Fact]
    public async Task DomainError_WhenThrown_ShouldReturnMappedProblemDetails()
    {
        using var client = factory.CreateClientWithEndpoint(endpoints =>
        {
            endpoints.MapGet("/domain-error", (HttpContext _) => throw new OrderAlreadyPaidError(Guid.NewGuid()));
        });

        var response = await client.GetAsync("/domain-error");
        var problem = await ReadProblemAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.GetProperty("type").GetString().Should().Be("https://httpstatuses.com/409");
        problem.GetProperty("title").GetString().Should().Be("Domain conflict");
        problem.GetProperty("detail").GetString().Should().Contain("already paid");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidationError_WhenThrown_ShouldReturnBadRequestProblemDetails()
    {
        using var client = factory.CreateClientWithEndpoint(endpoints =>
        {
            endpoints.MapGet("/validation-error", (HttpContext _) => throw new ArgumentException("Name is required."));
        });

        var response = await client.GetAsync("/validation-error");
        var problem = await ReadProblemAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.GetProperty("title").GetString().Should().Be("Validation error");
        problem.GetProperty("detail").GetString().Should().Contain("Name is required");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnexpectedError_WhenThrown_ShouldReturnSafeProblemDetails()
    {
        using var client = factory.CreateClientWithEndpoint(endpoints =>
        {
            endpoints.MapGet("/unexpected-error", (HttpContext _) => throw new InvalidOperationException("sensitive implementation detail"));
        });

        var response = await client.GetAsync("/unexpected-error");
        var problem = await ReadProblemAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        problem.GetProperty("title").GetString().Should().Be("Unexpected error");
        problem.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
        problem.GetProperty("detail").GetString().Should().NotContain("sensitive implementation detail");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return document!.RootElement.Clone();
    }
}

public sealed class ProblemDetailsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Replace Postgres with in-memory; no database interaction in these tests.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase($"TestDb-{Guid.NewGuid()}"));
        });
    }

    public HttpClient CreateClientWithEndpoint(Action<IEndpointRouteBuilder> mapEndpoints)
    {
        return WithWebHostBuilder(builder =>
        {
            builder.Configure(app =>
            {
                app.UseExceptionHandler();
                app.UseRouting();
                app.UseEndpoints(mapEndpoints);
            });
        }).CreateClient();
    }
}
