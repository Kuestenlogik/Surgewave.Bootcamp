// SurgewaveMart Analytics Service
// Real-time metrics aggregation with windowed processing

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Producer;
using Kuestenlogik.Surgewave.Client.Consumer;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== SurgewaveMart Analytics Service ===\n");

// Metrics state
var minuteMetrics = new ConcurrentDictionary<DateTime, MinuteMetrics>();
var hourlyMetrics = new ConcurrentDictionary<DateTime, HourlyMetrics>();
var topProducts = new ConcurrentDictionary<string, int>();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

Console.WriteLine("[Setup] Topics will be auto-created");

// Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "analytics-service";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe(Topics.Orders);

// TODO: Implement tumbling window aggregation (1 minute)
// - Count orders per minute
// - Sum revenue per minute
// - Calculate average order value

// TODO: Implement hopping window aggregation (1 hour, 5 min advance)
// - Hourly revenue trends
// - Top products by order count
// - Order volume trends

// TODO: Implement real-time dashboard updates
// - Emit aggregated metrics to analytics topic
// - Support WebSocket push for live updates

Console.WriteLine("[Consumer] Starting analytics processor...");

// Background task to emit periodic metrics
var metricsTask = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(1));

        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        // TODO: Emit minute metrics
        // if (minuteMetrics.TryGetValue(windowStart.AddMinutes(-1), out var metrics))
        // {
        //     var summary = new AnalyticsSummary(
        //         metrics.OrderCount,
        //         metrics.Revenue,
        //         metrics.OrderCount > 0 ? metrics.Revenue / metrics.OrderCount : 0,
        //         topProducts.OrderByDescending(x => x.Value).Take(5).ToDictionary(x => x.Key, x => x.Value),
        //         windowStart.AddMinutes(-1)
        //     );
        //
        //     await producer.ProduceAsync(Topics.Analytics, null, JsonSerializer.Serialize(summary));
        // }

        Console.WriteLine($"[Metrics] Window {windowStart:HH:mm} - Orders: {GetCurrentMinuteCount()}, Revenue: ${GetCurrentMinuteRevenue():F2}");
    }
});

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    // TODO: Consume from multiple topics (orders, payments, shipments)
    // and aggregate metrics

    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result is null) continue;

        try
        {
            var orderEvent = JsonSerializer.Deserialize<OrderCreated>(result.Value);
            if (orderEvent is null) continue;

            var windowStart = GetMinuteWindow(orderEvent.CreatedAt);

            // TODO: Update minute metrics
            // minuteMetrics.AddOrUpdate(
            //     windowStart,
            //     new MinuteMetrics(1, orderEvent.Total),
            //     (_, existing) => existing with
            //     {
            //         OrderCount = existing.OrderCount + 1,
            //         Revenue = existing.Revenue + orderEvent.Total
            //     });

            // TODO: Update product counts
            // foreach (var item in orderEvent.Items)
            // {
            //     topProducts.AddOrUpdate(item.ProductId, item.Quantity, (_, count) => count + item.Quantity);
            // }

            Console.WriteLine($"[Aggregated] Order {orderEvent.OrderId} - ${orderEvent.Total:F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to process analytics event: {ex.Message}");
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n[Shutdown] Analytics service stopping...");
}

// Print final metrics
Console.WriteLine("\n[Final] Analytics Summary:");
Console.WriteLine($"  Total Windows: {minuteMetrics.Count}");
Console.WriteLine($"  Top Products:");
foreach (var (productId, count) in topProducts.OrderByDescending(x => x.Value).Take(5))
{
    Console.WriteLine($"    {productId}: {count} units");
}

Console.WriteLine("=== Analytics Service Stopped ===");

// Helper methods
DateTime GetMinuteWindow(DateTime timestamp) =>
    new(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, 0);

int GetCurrentMinuteCount()
{
    var window = GetMinuteWindow(DateTime.UtcNow);
    return minuteMetrics.TryGetValue(window, out var m) ? m.OrderCount : 0;
}

decimal GetCurrentMinuteRevenue()
{
    var window = GetMinuteWindow(DateTime.UtcNow);
    return minuteMetrics.TryGetValue(window, out var m) ? m.Revenue : 0;
}

// Metrics records
record MinuteMetrics(int OrderCount, decimal Revenue);
record HourlyMetrics(int OrderCount, decimal Revenue, Dictionary<string, int> TopProducts);
record AnalyticsSummary(int OrderCount, decimal Revenue, decimal AvgOrderValue, Dictionary<string, int> TopProducts, DateTime WindowStart);