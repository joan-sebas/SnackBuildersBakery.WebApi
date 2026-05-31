using Api.Auth;
using Api.Contracts;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Api.IntegrationTests;

[Collection("ApiDb")]
public sealed class KitchenMonitoringEndpointsTests(ApiDbFactory factory)
{
    private HttpClient ManagerClient()
    {
        var c = factory.CreateClient();
        c.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, ApiDbFactory.ManagerKey);
        return c;
    }

    private HttpClient PublicClient()
    {
        var c = factory.CreateClient();
        c.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, ApiDbFactory.PublicKey);
        return c;
    }

    [Fact]
    public async Task GetKitchenSnapshot_ManagerClient_Returns200WithSnapshot()
    {
        using var client = ManagerClient();
        var response = await client.GetAsync("/v1/kitchen");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await response.Content.ReadFromJsonAsync<KitchenSnapshotResponse>(TestJsonOptions.Default);
        snapshot.Should().NotBeNull();
        snapshot!.Slots.Should().NotBeNull();
        snapshot.Queue.Should().NotBeNull();
    }

    [Fact]
    public async Task GetKitchenSnapshot_PublicClient_Returns403()
    {
        using var client = PublicClient();
        var response = await client.GetAsync("/v1/kitchen");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetKitchenSnapshot_Anonymous_Returns401()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/v1/kitchen");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
