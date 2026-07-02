// Unit 1 Project: Order Notification System
// Build a real-time order notification system for e-commerce

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Order Notification System ===\n");

// Statistics tracking
var stats = new ConcurrentDictionary<int, int>();
var totalProcessed = 0;
var totalAmount = 0m;

// Start embedded broker using SurgewaveRuntime
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)  // Auto-assign port
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on {runtime.BootstrapServers}");

// TODO: Create producer using SurgewaveProducer<string, string>
// var producer = new SurgewaveProducer<string, string>(options => { ... });

// TODO: Create topics using client.Topics.CreateAsync("orders", partitions: 3);
// You can access the NativeClient for topic creation

// Sample orders to produce
var orders = new List<OrderEvent>
{
    new("ORD-001", "C-123", new() { new("P1", "Widget", 2, 50.00m) }, 125.50m, DateTime.UtcNow, OrderPriority.Normal),
    new("ORD-002", "C-456", new() { new("P2", "Gadget", 1, 750.00m) }, 750.00m, DateTime.UtcNow, OrderPriority.High),
    new("ORD-003", "C-789", new() { new("P3", "Tool", 3, 15.00m) }, 45.00m, DateTime.UtcNow, OrderPriority.Normal),
    new("ORD-004", "C-123", new() { new("P4", "Device", 1, 299.99m) }, 299.99m, DateTime.UtcNow, OrderPriority.Normal),
    new("ORD-005", "C-456", new() { new("P5", "Premium", 2, 400.00m) }, 800.00m, DateTime.UtcNow, OrderPriority.Express),
};

// TODO: Step 1 - Produce orders
// - Use CustomerId as the key for partitioning
// - Serialize OrderEvent to JSON
// - Print partition assignment for each order
Console.WriteLine("[Producer] Starting order generation...");

foreach (var order in orders)
{
    // TODO: Produce order to "orders" topic
    // Hint: await producer.ProduceAsync("orders", order.CustomerId, JsonSerializer.Serialize(order))

    Console.WriteLine($"[Producer] Order {order.OrderId} (${order.Total}) produced");
}

// TODO: Step 2 - Consume and process orders
// - Create consumer using SurgewaveConsumer<string, string>
// - Subscribe to "orders" topic
// - Deserialize JSON to OrderEvent
// - Track statistics per partition
// - Route high-value orders (Total > 500) to "orders-priority"
Console.WriteLine("\n[Consumer] Starting order processing...");

// TODO: Create consumer
// var consumer = new SurgewaveConsumer<string, string>(options => { ... });
// consumer.Subscribe("orders");

// TODO: Consume orders using consumer.ConsumeAsync(timeout)
// For each order:
//   1. Deserialize the order
//   2. Log processing
//   3. If Total > 500, produce to "orders-priority"
//   4. Update statistics

// TODO: Step 3 - Print statistics
Console.WriteLine("\n[Stats] Order Processing Summary:");
Console.WriteLine($"[Stats] Processed: {totalProcessed} orders, Total: ${totalAmount:F2}");
foreach (var (partition, count) in stats.OrderBy(x => x.Key))
{
    Console.WriteLine($"[Stats] Partition {partition}: {count} orders");
}

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
