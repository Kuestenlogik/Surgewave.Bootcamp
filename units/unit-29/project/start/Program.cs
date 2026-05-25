// Unit 11 Project: E-Commerce Backend
// Build a complete Order -> Payment -> Shipping event flow

using System.Collections.Concurrent;
using System.Text.Json;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== E-Commerce Event Processing System ===\n");

// Event types
public record OrderCreated(
    string OrderId,
    string CustomerId,
    List<OrderItem> Items,
    decimal TotalAmount,
    DateTime CreatedAt,
    string CorrelationId
);

public record PaymentProcessed(
    string PaymentId,
    string OrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTime ProcessedAt,
    string CorrelationId
);

public record ShipmentCreated(
    string ShipmentId,
    string OrderId,
    string Address,
    DateTime EstimatedDelivery,
    string CorrelationId
);

public record OrderItem(string ProductId, string Name, int Quantity, decimal Price);

public enum PaymentStatus { Approved, Declined, Pending }

// Analytics counters
var analyticsCounters = new ConcurrentDictionary<string, int>();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// TODO: Step 1 - Create producers and consumers
// Topics needed:
// - "orders-created"
// - "payments-processed"
// - "payments-failed" - Dead Letter Queue
// - "shipments-created"
// - "events-analytics"

Console.WriteLine("[Setup] Creating producers and consumers...\n");

// TODO: Create producers using SurgewaveProducer<string, string>
// Example:
// await using var orderProducer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

// TODO: Create consumers using SurgewaveConsumer<string, string>
// Example:
// await using var orderConsumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
//     opt.GroupId = "payment-service";
//     opt.AutoOffsetReset = Kuestenlogik.Surgewave.Client.Consumer.AutoOffsetReset.Earliest;
// });
// orderConsumer.Subscribe("orders-created");


// Sample orders to process
var orders = new List<OrderCreated>
{
    new("ORD-001", "C-123", new() { new("P1", "Laptop Stand", 1, 299.99m) }, 299.99m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-002", "C-456", new() { new("P2", "Gaming PC", 1, 1250.00m) }, 1250.00m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-003", "C-789", new() { new("P3", "USB Cable", 2, 24.99m) }, 49.99m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-004", "C-123", new() { new("P4", "Monitor", 1, 450.00m) }, 450.00m, DateTime.UtcNow, Guid.NewGuid().ToString()),
    new("ORD-005", "C-456", new() { new("P5", "Keyboard", 1, 89.99m) }, 89.99m, DateTime.UtcNow, Guid.NewGuid().ToString()),
};

// TODO: Step 2 - Order Service: Produce OrderCreated events
Console.WriteLine("[Order Service] Starting...");

foreach (var order in orders)
{
    // TODO: Produce order to "orders-created" topic
    // - Use OrderId as key
    // - Serialize to JSON using JsonSerializer.Serialize(order)
    // - Also send to "events-analytics" for tracking
    // Example:
    // await orderProducer.ProduceAsync("orders-created", order.OrderId, JsonSerializer.Serialize(order));

    Console.WriteLine($"[Order Service] Order {order.OrderId} created for customer {order.CustomerId} (${order.TotalAmount:F2})");
}

Console.WriteLine();

// TODO: Step 3 - Payment Service: Consume orders and process payments
Console.WriteLine("[Payment Service] Starting...");

// TODO: Consume from "orders-created"
// For each order:
//   1. Simulate payment processing (delay 100ms)
//   2. If TotalAmount > 1000, simulate payment failure (Declined)
//   3. Produce PaymentProcessed to "payments-processed" or "payments-failed"
//   4. Send to "events-analytics"
//
// Example consume loop:
// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
// while (!cts.IsCancellationRequested)
// {
//     var result = await orderConsumer.ConsumeAsync(cts.Token);
//     if (result is null) continue;
//     var order = JsonSerializer.Deserialize<OrderCreated>(result.Value);
//     // Process order...
// }

// Hint: Use this pattern for payment simulation
// var paymentSuccess = order.TotalAmount <= 1000;
// var status = paymentSuccess ? PaymentStatus.Approved : PaymentStatus.Declined;

Console.WriteLine();

// TODO: Step 4 - Shipping Service: Consume successful payments and create shipments
Console.WriteLine("[Shipping Service] Starting...");

// TODO: Consume from "payments-processed"
// For each successful payment:
//   1. Create ShipmentCreated event
//   2. Produce to "shipments-created"
//   3. Send to "events-analytics"

// Hint: Use estimated delivery 3-5 days from now
// var estimatedDelivery = DateTime.UtcNow.AddDays(Random.Shared.Next(3, 6));

Console.WriteLine();

// TODO: Step 5 - Analytics Dashboard
Console.WriteLine("[Analytics] Event Summary:");
Console.WriteLine($"[Analytics] Orders: {analyticsCounters.GetValueOrDefault("orders", 0)}");
Console.WriteLine($"[Analytics] Payments Approved: {analyticsCounters.GetValueOrDefault("payments_approved", 0)}");
Console.WriteLine($"[Analytics] Payments Declined: {analyticsCounters.GetValueOrDefault("payments_declined", 0)}");
Console.WriteLine($"[Analytics] Shipments: {analyticsCounters.GetValueOrDefault("shipments", 0)}");

Console.WriteLine("\n=== E-Commerce Event Processing Complete ===");
