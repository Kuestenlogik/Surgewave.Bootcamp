// Unit 1 Project: Order Notification System - COMPLETED
// A real-time order notification system for e-commerce

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Order Notification System ===\n");

// Statistics tracking
var stats = new ConcurrentDictionary<int, int>();
var totalProcessed = 0;
var totalAmount = 0m;
var priorityCount = 0;

// Start embedded broker using SurgewaveRuntime
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)  // Auto-assign port
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(false)  // We'll create topics explicitly
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on {runtime.BootstrapServers}");

// Create native client for topic creation
await using var nativeClient = new SurgewaveNativeClient("localhost", runtime.Port);
await nativeClient.ConnectAsync();

// Create topics
await nativeClient.Topics.CreateAsync("orders", partitions: 3);
await nativeClient.Topics.CreateAsync("orders-priority", partitions: 1);

Console.WriteLine("[Setup] Topics created: orders (3 partitions), orders-priority (1 partition)\n");

// Create producer
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = runtime.BootstrapServers;
});

// Sample orders to produce
var orders = new List<OrderEvent>
{
    new("ORD-001", "C-123", new() { new("P1", "Widget", 2, 50.00m) }, 125.50m, DateTime.UtcNow, OrderPriority.Normal),
    new("ORD-002", "C-456", new() { new("P2", "Gadget", 1, 750.00m) }, 750.00m, DateTime.UtcNow, OrderPriority.High),
    new("ORD-003", "C-789", new() { new("P3", "Tool", 3, 15.00m) }, 45.00m, DateTime.UtcNow, OrderPriority.Normal),
    new("ORD-004", "C-123", new() { new("P4", "Device", 1, 299.99m) }, 299.99m, DateTime.UtcNow, OrderPriority.Normal),
    new("ORD-005", "C-456", new() { new("P5", "Premium", 2, 400.00m) }, 800.00m, DateTime.UtcNow, OrderPriority.Express),
};

// Step 1: Produce orders
Console.WriteLine("[Producer] Starting order generation...");

foreach (var order in orders)
{
    var json = JsonSerializer.Serialize(order);
    var result = await producer.ProduceAsync("orders", order.CustomerId, json);

    var priorityTag = order.Total > 500 ? " [HIGH PRIORITY]" : "";
    Console.WriteLine($"[Producer] Order {order.OrderId} (${order.Total:F2}) -> Partition {result.Partition}{priorityTag}");
}

Console.WriteLine($"[Producer] Produced {orders.Count} orders\n");

// Step 2: Consume and process orders
Console.WriteLine("[Consumer] Starting order processing...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = runtime.BootstrapServers;
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("orders");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    while (totalProcessed < orders.Count)
    {
        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(1), cts.Token);

        if (result == null) continue;

        var order = JsonSerializer.Deserialize<OrderEvent>(result.Value!);
        if (order is null) continue;

        // Track statistics
        stats.AddOrUpdate(result.Partition, 1, (_, count) => count + 1);
        Interlocked.Increment(ref totalProcessed);
        totalAmount += order.Total;

        Console.WriteLine($"[Consumer] Processing Order {order.OrderId} for customer {order.CustomerId} (Partition {result.Partition})");

        // Route high-value orders to priority topic
        if (order.Total > 500)
        {
            await producer.ProduceAsync("orders-priority", order.OrderId, result.Value!);

            Interlocked.Increment(ref priorityCount);
            Console.WriteLine($"[Priority] High-value order {order.OrderId} (${order.Total:F2}) routed to priority queue");
        }
    }
}
catch (OperationCanceledException)
{
    // Expected timeout
}

// Step 3: Print statistics
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("[Stats] Order Processing Summary:");
Console.WriteLine($"[Stats] Processed: {totalProcessed} orders");
Console.WriteLine($"[Stats] Total Value: ${totalAmount:F2}");
Console.WriteLine($"[Stats] Priority Orders: {priorityCount}");
Console.WriteLine();
Console.WriteLine("[Stats] Orders per Partition:");
foreach (var (partition, count) in stats.OrderBy(x => x.Key))
{
    Console.WriteLine($"  Partition {partition}: {count} orders");
}

Console.WriteLine(new string('=', 50));
Console.WriteLine("\n=== Order Notification System Complete ===");

// Order event types (must be at end of file for top-level statements)
public record OrderEvent(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal Total,
    DateTime CreatedAt,
    OrderPriority Priority
);

public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);

public enum OrderPriority { Normal, High, Express }
