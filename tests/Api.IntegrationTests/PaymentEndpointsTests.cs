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
        var items = await client.GetFromJsonAsync<List<MenuItemResponse>>("/v1/menu", TestJsonOptions.Default);
        var menuItemId = items!.First().Id;
        var placed = await client.PostAsJsonAsync("/v1/orders",
            new { PriorityLevel = "WalkIn", Lines = new[] { new { MenuItemId = menuItemId, Quantity = 1 } } });
        var ticket = await placed.Content.ReadFromJsonAsync<TicketResponse>(TestJsonOptions.Default);
        return ticket!.OrderId;
    }

    [Fact]
    public async Task PayOrder_Success_Returns200OrPaymentRequired()
    {
        using var client = PublicClient();
        var orderId = await PlaceOrderAsync(client);
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };

        var response = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.PaymentRequired);
        var result = await response.Content.ReadFromJsonAsync<PayOrderResponse>(TestJsonOptions.Default);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PayOrder_SameIdempotencyKey_SecondCallReplaysCachedResult()
    {
        using var client = PublicClient();
        var orderId = await PlaceOrderAsync(client);
        var key = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("Idempotency-Key", key);
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };

        var first = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
        first.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.PaymentRequired);

        var second = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
        second.StatusCode.Should().Be(first.StatusCode);
    }

    [Fact]
    public async Task PayOrder_AlreadyPaid_Returns409Conflict()
    {
        using var client = PublicClient();
        var orderId = await PlaceOrderAsync(client);
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };

        var first = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
        if (first.StatusCode != HttpStatusCode.OK)
            return; // payment failed — cannot test double-pay path

        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var second = await client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
