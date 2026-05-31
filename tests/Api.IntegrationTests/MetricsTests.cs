using Api.Auth;
using Api.Contracts;
using Api.Metrics;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;

namespace Api.IntegrationTests;

[Collection("ApiDb")]
public sealed class MetricsTests(ApiDbFactory factory)
{
    [Fact]
    public async Task PlaceOrder_WhenCreated_ShouldIncrementOrdersPlacedCounter()
    {
        using var recorder = new MetricsRecorder();
        using var client = CreatePublicClient(factory);

        await PlaceOrderAsync(client);

        recorder.Measurements.Should().Contain(metric =>
            metric.Name == SnackBuildersMetrics.OrdersPlacedName &&
            metric.Value == 1);
    }

    [Fact]
    public async Task PayOrder_WhenSuccessful_ShouldIncrementSuccessOutcomeCounter()
    {
        using var client = CreatePublicClient(factory);
        var ticket = await PlaceOrderAsync(client);
        using var recorder = new MetricsRecorder();

        var response = await PayOrderAsync(client, ticket.OrderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        recorder.Measurements.Should().Contain(metric =>
            metric.Name == SnackBuildersMetrics.PaymentsProcessedName &&
            metric.Value == 1 &&
            metric.Tags["outcome"] == "success");
    }

    [Fact]
    public async Task PayOrder_WhenDeclined_ShouldIncrementFailureOutcomeCounter()
    {
        using var host = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PaymentGateway:CashFailureRate"] = "1.0"
                })));
        using var client = CreatePublicClient(host);
        var ticket = await PlaceOrderAsync(client);
        using var recorder = new MetricsRecorder();

        var response = await PayOrderAsync(client, ticket.OrderId);

        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        recorder.Measurements.Should().Contain(metric =>
            metric.Name == SnackBuildersMetrics.PaymentsProcessedName &&
            metric.Value == 1 &&
            metric.Tags["outcome"] == "failure");
    }

    [Fact]
    public async Task SchedulerMetrics_WhenQueueHasWaitingItems_ShouldReportQueueDepthAndOccupiedSlots()
    {
        using var client = CreatePublicClient(factory);
        var ticket = await PlaceOrderAsync(client, quantity: 9);
        using var recorder = new MetricsRecorder();

        var response = await PayOrderAsync(client, ticket.OrderId);
        recorder.RecordObservableInstruments();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        recorder.Measurements.Should().Contain(metric =>
            metric.Name == SnackBuildersMetrics.QueueDepthName &&
            metric.Value > 0);
        recorder.Measurements.Should().Contain(metric =>
            metric.Name == SnackBuildersMetrics.SlotsOccupiedName &&
            metric.Value > 0);
    }

    [Fact]
    public async Task SchedulerMetrics_WhenItemTransitionsToReady_ShouldIncrementItemsBakedCounter()
    {
        using var host = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Scheduler:Turnover"] = "00:00:00",
                    ["Scheduler:BakeTimes:Cookie"] = "00:00:00",
                    ["Scheduler:BakeTimes:Pastry"] = "00:00:00",
                    ["Scheduler:BakeTimes:Bread"] = "00:00:00"
                })));
        using var client = CreatePublicClient(host);
        var ticket = await PlaceOrderAsync(client, quantity: 2);
        using var recorder = new MetricsRecorder();

        var response = await PayOrderAsync(client, ticket.OrderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        recorder.Measurements.Should().Contain(metric =>
            metric.Name == SnackBuildersMetrics.ItemsBakedName &&
            metric.Value > 0);
    }

    private static HttpClient CreatePublicClient(WebApplicationFactory<Program> host)
    {
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, ApiDbFactory.PublicKey);
        return client;
    }

    private static async Task<TicketResponse> PlaceOrderAsync(HttpClient client, int quantity = 1)
    {
        var items = await client.GetFromJsonAsync<List<MenuItemResponse>>("/v1/menu", TestJsonOptions.Default);
        var body = new
        {
            PriorityLevel = "WalkIn",
            Lines = new[] { new { MenuItemId = items!.First().Id, Quantity = quantity } }
        };

        var response = await client.PostAsJsonAsync("/v1/orders", body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketResponse>(TestJsonOptions.Default))!;
    }

    private static Task<HttpResponseMessage> PayOrderAsync(HttpClient client, Guid orderId)
    {
        var body = new { Method = "Cash", Amount = 100.00m, Currency = "USD" };
        return client.PostAsJsonAsync($"/v1/orders/{orderId}/payment", body);
    }

    private sealed class MetricsRecorder : IDisposable
    {
        private readonly object _sync = new();
        private readonly MeterListener _listener = new();
        private readonly List<RecordedMetric> _measurements = [];

        public MetricsRecorder()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == SnackBuildersMetrics.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            };

            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
                AddMeasurement(instrument.Name, value, tags));
            _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) =>
                AddMeasurement(instrument.Name, value, tags));
            _listener.Start();
        }

        public IReadOnlyList<RecordedMetric> Measurements
        {
            get
            {
                lock (_sync)
                    return [.. _measurements];
            }
        }

        public void RecordObservableInstruments() => _listener.RecordObservableInstruments();

        public void Dispose() => _listener.Dispose();

        private void AddMeasurement<T>(
            string name,
            T value,
            ReadOnlySpan<KeyValuePair<string, object?>> tags)
            where T : struct
        {
            var measurement = Convert.ToInt64(value);
            var capturedTags = CaptureTags(tags);

            lock (_sync)
                _measurements.Add(new RecordedMetric(name, measurement, capturedTags));
        }

        private static IReadOnlyDictionary<string, string?> CaptureTags(
            ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var captured = new Dictionary<string, string?>();
            foreach (var tag in tags)
                captured[tag.Key] = tag.Value?.ToString();

            return captured;
        }
    }

    private sealed record RecordedMetric(
        string Name,
        long Value,
        IReadOnlyDictionary<string, string?> Tags);
}
