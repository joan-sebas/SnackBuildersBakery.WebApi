using Api.Auth;
using Api.Contracts;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Api.IntegrationTests;

[Collection("ApiDb")]
public sealed class MenuEndpointsTests(ApiDbFactory factory)
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
    public async Task ListMenuItems_PublicClient_ReturnsSeededItems()
    {
        using var client = PublicClient();
        var response = await client.GetAsync("/v1/menu");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<MenuItemResponse>>();
        items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMenuItem_KnownId_ReturnsItem()
    {
        using var client = PublicClient();
        var list = await client.GetFromJsonAsync<List<MenuItemResponse>>("/v1/menu");
        var id = list!.First().Id;

        var response = await client.GetAsync($"/v1/menu/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await response.Content.ReadFromJsonAsync<MenuItemResponse>();
        item!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetMenuItem_UnknownId_Returns404()
    {
        using var client = PublicClient();
        var response = await client.GetAsync($"/v1/menu/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateMenuItem_ManagerClient_Returns201WithLocation()
    {
        using var client = ManagerClient();
        var body = new { Name = "Test Muffin", SnackType = "Cookie", Price = 2.50m, Currency = "USD" };
        var response = await client.PostAsJsonAsync("/v1/menu", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var item = await response.Content.ReadFromJsonAsync<MenuItemResponse>();
        item!.Name.Should().Be("Test Muffin");
    }

    [Fact]
    public async Task CreateMenuItem_PublicClient_Returns403()
    {
        using var client = PublicClient();
        var body = new { Name = "Forbidden", SnackType = "Cookie", Price = 1.00m, Currency = "USD" };
        var response = await client.PostAsJsonAsync("/v1/menu", body);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateMenuItem_EmptyName_Returns400ValidationProblem()
    {
        using var client = ManagerClient();
        var body = new { Name = "", SnackType = "Cookie", Price = 1.00m, Currency = "USD" };
        var response = await client.PostAsJsonAsync("/v1/menu", body);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task DeleteMenuItem_ManagerClient_Returns204()
    {
        using var client = ManagerClient();
        var created = await client.PostAsJsonAsync("/v1/menu",
            new { Name = "To Delete", SnackType = "Bread", Price = 5.00m, Currency = "USD" });
        var item = await created.Content.ReadFromJsonAsync<MenuItemResponse>();

        var response = await client.DeleteAsync($"/v1/menu/{item!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteMenuItem_UnknownId_Returns404()
    {
        using var client = ManagerClient();
        var response = await client.DeleteAsync($"/v1/menu/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
