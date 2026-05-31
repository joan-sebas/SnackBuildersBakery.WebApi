using Api.Auth;
using Api.Contracts;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Api.IntegrationTests;

[Collection("ApiDb")]
public sealed class OrderEndpointsTests(ApiDbFactory factory)
{
    private HttpClient PublicClient()
    {
        var c = factory.CreateClient();
        c.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, ApiDbFactory.PublicKey);
        return c;
    }

    private async Task<Guid> GetFirstMenuItemIdAsync(HttpClient client)
    {
        var items = await client.GetFromJsonAsync<List<MenuItemResponse>>("/v1/menu");
        return items!.First().Id;
    }

    [Fact]
    public async Task PlaceOrder_ValidBody_Returns201WithTicket()
    {
        using var client = PublicClient();
        var menuItemId = await GetFirstMenuItemIdAsync(client);
        var body = new
        {
            PriorityLevel = "WalkIn",
            Lines = new[] { new { MenuItemId = menuItemId, Quantity = 2 } }
        };

        var response = await client.PostAsJsonAsync("/v1/orders", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        ticket!.OrderId.Should().NotBeEmpty();
        ticket.TotalPrice.Should().BeGreaterThan(0);
        ticket.IsEstimateSubjectToPayment.Should().BeTrue();
    }

    [Fact]
    public async Task PlaceOrder_EmptyLines_Returns400()
    {
        using var client = PublicClient();
        var body = new { PriorityLevel = "WalkIn", Lines = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync("/v1/orders", body);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_OffMenuItemId_Returns422()
    {
        using var client = PublicClient();
        var body = new
        {
            PriorityLevel = "WalkIn",
            Lines = new[] { new { MenuItemId = Guid.NewGuid(), Quantity = 1 } }
        };
        var response = await client.PostAsJsonAsync("/v1/orders", body);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task PlaceOrder_SameIdempotencyKey_ReturnsSameTicket()
    {
        using var client = PublicClient();
        var menuItemId = await GetFirstMenuItemIdAsync(client);
        var key = Guid.NewGuid().ToString();
        var body = new
        {
            PriorityLevel = "WalkIn",
            Lines = new[] { new { MenuItemId = menuItemId, Quantity = 1 } }
        };

        client.DefaultRequestHeaders.Add("Idempotency-Key", key);
        var first = await client.PostAsJsonAsync("/v1/orders", body);
        var second = await client.PostAsJsonAsync("/v1/orders", body);

        var t1 = await first.Content.ReadFromJsonAsync<TicketResponse>();
        var t2 = await second.Content.ReadFromJsonAsync<TicketResponse>();
        t2!.TicketId.Should().Be(t1!.TicketId);
        t2.OrderId.Should().Be(t1.OrderId);
    }

    [Fact]
    public async Task TrackOrder_KnownOrder_ReturnsItemStatuses()
    {
        using var client = PublicClient();
        var menuItemId = await GetFirstMenuItemIdAsync(client);
        var placed = await client.PostAsJsonAsync("/v1/orders",
            new { PriorityLevel = "WalkIn", Lines = new[] { new { MenuItemId = menuItemId, Quantity = 1 } } });
        var ticket = await placed.Content.ReadFromJsonAsync<TicketResponse>();

        var response = await client.GetAsync($"/v1/orders/{ticket!.OrderId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracking = await response.Content.ReadFromJsonAsync<TrackOrderResponse>();
        tracking!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TrackOrder_UnknownId_Returns404()
    {
        using var client = PublicClient();
        var response = await client.GetAsync($"/v1/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
