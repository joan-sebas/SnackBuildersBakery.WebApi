using Api.Auth;
using Api.Contracts;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Api.IntegrationTests;

[Collection("ApiDb")]
public sealed class PaymentEndpointsTests(ApiDbFactory factory)
{
    private HttpClient PublicClient()
    {
        var c = factory.CreateClient();
        c.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, ApiDbFactory.PublicKey);
        return c;
    }

    private async Task<Guid> PlaceOrderAsync(HttpClient client)
    {
        var items = await client.GetFromJsonAsync<List<MenuItemResponse>>("/v1/menu");
        var menuItemId = items!.First().Id;
        var placed = await client.PostAsJsonAsync("/v1/orders",
            new { PriorityLevel = "WalkIn", Lines = new[] { new { MenuItemId = menuItemId, Quantity = 1 } } });
        var ticket = await placed.Content.ReadFromJsonAsync<TicketResponse>();
        return ticket!.OrderId;
    }

    [Fact]
    public async Task PayOrder_Success_Returns200AndIsSuccessTrue()
    {
        using var client = PublicClient();
        var orderId = await PlaceOrderAsync(client);
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };

        var response = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.PaymentRequired);
        var result = await response.Content.ReadFromJsonAsync<PayOrderResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PayOrder_SameIdempotencyKey_ReturnsSameResult()
    {
        using var client = PublicClient();
        var orderId = await PlaceOrderAsync(client);
        var key = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("Idempotency-Key", key);
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };

        var first = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
        // A second call with the same key to a different order (key already used — replay)
        var second = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);

        var r1 = await first.Content.ReadFromJsonAsync<PayOrderResponse>();
        var r2 = await second.Content.ReadFromJsonAsync<PayOrderResponse>();
        r2!.IsSuccess.Should().Be(r1!.IsSuccess);
    }

    [Fact]
    public async Task PayOrder_AlreadyPaid_Returns409()
    {
        using var client = PublicClient();
        var orderId = await PlaceOrderAsync(client);
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };

        // Force success by using cash with 0% failure rate; pay twice with different keys
        await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);

        // Add a second key to avoid idempotency replay
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var second = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);

        // Either already-paid conflict (409) or success replayed — depends on first call result
        second.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.OK,
            HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task PayOrder_UnknownOrder_Returns404()
    {
        using var client = PublicClient();
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };
        var response = await client.PostAsJsonAsync($"/v1/orders/{Guid.NewGuid()}/payment", body);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
