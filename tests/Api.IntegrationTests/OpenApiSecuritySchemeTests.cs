using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace Api.IntegrationTests;

[Collection("ApiDb")]
public sealed class OpenApiSecuritySchemeTests(ApiDbFactory factory)
{
    [Fact]
    public async Task OpenApiDocument_DeclaresApiKeySecurityScheme()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var scheme = doc.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("ApiKey");

        scheme.GetProperty("type").GetString().Should().Be("apiKey");
        scheme.GetProperty("in").GetString().Should().Be("header");
        scheme.GetProperty("name").GetString().Should().Be("X-Api-Key");
    }

    [Fact]
    public async Task OpenApiDocument_MarksKitchenEndpointAsSecured()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var get = doc.RootElement.GetProperty("paths").GetProperty("/v1/kitchen").GetProperty("get");
        get.TryGetProperty("security", out var security).Should().BeTrue();
        security.EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task OpenApiDocument_LeavesPublicOrderRouteUnsecured()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var post = doc.RootElement.GetProperty("paths").GetProperty("/v1/orders").GetProperty("post");
        post.TryGetProperty("security", out _).Should().BeFalse();
    }
}
