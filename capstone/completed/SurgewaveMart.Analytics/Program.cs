// SurgewaveMart Analytics Service - COMPLETED
// Real-time metrics aggregation with windowed processing

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using SurgewaveMart.Shared;
using SurgewaveMart.Shared.Events;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== SurgewaveMart Analytics Service ===\n");

// Metrics state - tumbling windows (1 minute)
var minuteMetrics = new ConcurrentDictionary<DateTime, MinuteWindow>();

// Metrics state - hopping windows (1 hour, 5 min advance)
var hourlyMetrics = new ConcurrentDictionary<DateTime, HourWindow>();

// Product statistics
var productCounts = new ConcurrentDictionary<string, int>();
var productRevenue = new ConcurrentDictionary<string, decimal>();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
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

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Background task to emit periodic metrics
var metricsEmitter = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (cts.Token.IsCancellationRequested) break;

        var now = DateTime.UtcNow;
        var windowStart = GetMinuteWindow(now.AddMinutes(-1));

        // Emit minute metrics
        if (minuteMetrics.TryGetValue(windowStart, out var metrics))
        {
            var summary = new AnalyticsSummary(
                WindowType: "Minute",
                WindowStart: windowStart,
                WindowEnd: windowStart.AddMinutes(1),
                OrderCount: metrics.OrderCount,
                Revenue: metrics.Revenue,
                AvgOrderValue: metrics.OrderCount > 0 ? metrics.Revenue / metrics.OrderCount : 0,
                TopProducts: productCounts
                    .OrderByDescending(x => x.Value)
                    .Take(5)
                    .Select(x => new ProductStat(x.Key, x.Value, productRevenue.GetValueOrDefault(x.Key)))
                    .ToList(),
                Timestamp: DateTime.UtcNow
            );

            await producer.ProduceAsync(Topics.Analytics, null, JsonSerializer.Serialize(summary));

            Console.WriteLine($"[Emit] Window {windowStart:HH:mm} - Orders: {metrics.OrderCount}, Revenue: ${metrics.Revenue:F2}");
        }

        // Emit hourly summary every 5 minutes
        if (now.Minute % 5 == 0)
        {
            var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            EmitHourlySummary(hourStart);
        }
    }
});

Console.WriteLine("[Consumer] Starting analytics processor...\n");

var totalOrders = 0;
var totalRevenue = 0m;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result is null) continue;

        try
        {
            using var doc = JsonDocument.Parse(result.Value);
            var eventType = doc.RootElement.GetProperty("EventType").GetString();

            if (eventType == "OrderCreated")
            {
                var order = JsonSerializer.Deserialize<OrderCreated>(result.Value)!;
                ProcessOrderCreated(order);
                totalOrders++;
                totalRevenue += order.Total;

                Console.WriteLine($"[Aggregated] Order {order.OrderId}: ${order.Total:F2}");
            }
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

// Print final summary
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Stats] Analytics Summary:");
Console.WriteLine($"  Total Orders:   {totalOrders}");
Console.WriteLine($"  Total Revenue:  ${totalRevenue:F2}");
Console.WriteLine($"  Avg Order Value: ${(totalOrders > 0 ? totalRevenue / totalOrders : 0):F2}");
Console.WriteLine();
Console.WriteLine("  Top Products:");
foreach (var (productId, count) in productCounts.OrderByDescending(x => x.Value).Take(5))
{
    var revenue = productRevenue.GetValueOrDefault(productId);
    Console.WriteLine($"    {productId}: {count} units, ${revenue:F2}");
}
Console.WriteLine(new string('=', 50));

Console.WriteLine("=== Analytics Service Stopped ===");

// Helper methods
void ProcessOrderCreated(OrderCreated order)
{
    var windowStart = GetMinuteWindow(order.CreatedAt);

    // Update minute metrics
    minuteMetrics.AddOrUpdate(
        windowStart,
        new MinuteWindow(1, order.Total),
        (_, existing) => new MinuteWindow(
            existing.OrderCount + 1,
            existing.Revenue + order.Total
        ));

    // Update hourly metrics
    var hourStart = new DateTime(order.CreatedAt.Year, order.CreatedAt.Month, order.CreatedAt.Day, order.CreatedAt.Hour, 0, 0);
    hourlyMetrics.AddOrUpdate(
        hourStart,
        new HourWindow(1, order.Total, new()),
        (_, existing) =>
        {
            existing.ProductCounts.Clear();
            foreach (var item in order.Items)
            {
                existing.ProductCounts.AddOrUpdate(item.ProductId, item.Quantity, (_, c) => c + item.Quantity);
            }
            return new HourWindow(
                existing.OrderCount + 1,
                existing.Revenue + order.Total,
                existing.ProductCounts
            );
        });

    // Update product statistics
    foreach (var item in order.Items)
    {
        productCounts.AddOrUpdate(item.ProductId, item.Quantity, (_, count) => count + item.Quantity);
        productRevenue.AddOrUpdate(item.ProductId, item.Quantity * item.UnitPrice,
            (_, rev) => rev + item.Quantity * item.UnitPrice);
    }
}

DateTime GetMinuteWindow(DateTime timestamp) =>
    new(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, 0);

void EmitHourlySummary(DateTime hourStart)
{
    if (!hourlyMetrics.TryGetValue(hourStart, out var metrics)) return;

    Console.WriteLine($"[Hourly] {hourStart:yyyy-MM-dd HH:00} - Orders: {metrics.OrderCount}, Revenue: ${metrics.Revenue:F2}");
}

// Records
record MinuteWindow(int OrderCount, decimal Revenue);
record HourWindow(int OrderCount, decimal Revenue, ConcurrentDictionary<string, int> ProductCounts);
record AnalyticsSummary(
    string WindowType,
    DateTime WindowStart,
    DateTime WindowEnd,
    int OrderCount,
    decimal Revenue,
    decimal AvgOrderValue,
    List<ProductStat> TopProducts,
    DateTime Timestamp
);
record ProductStat(string ProductId, int UnitsSold, decimal Revenue);