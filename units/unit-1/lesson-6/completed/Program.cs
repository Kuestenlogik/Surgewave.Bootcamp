// Surgewave Bootcamp - Unit 1, Lesson 6: Message Best Practices
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using System.Text.Json;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.6 - Message Best Practices ===");
Console.WriteLine();

// Step 1 - Start the embedded broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Demonstrate Events vs Commands
Console.WriteLine("--- Demonstrating Events vs Commands ---");
Console.WriteLine();

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Generate a correlation ID that will follow this entire business transaction
var correlationId = Guid.NewGuid();
var customerId = Guid.NewGuid();

// Create a command (a request for action)
var placeOrderCommand = new PlaceOrder(
    CustomerId: customerId,
    Items: new List<OrderItem>
    {
        new(Guid.NewGuid(), 2, 29.99m),
        new(Guid.NewGuid(), 1, 39.99m)
    },
    CorrelationId: correlationId);

Console.WriteLine("Publishing command: PlaceOrder");
Console.WriteLine($"  CorrelationId: {correlationId}");
Console.WriteLine($"  CustomerId: {customerId}");

await producer.ProduceAsync(
    "commands",
    customerId.ToString(),
    JsonSerializer.Serialize(placeOrderCommand));

Console.WriteLine();

// Simulate command handler creating an event (fact about what happened)
var orderId = Guid.NewGuid();
var totalAmount = placeOrderCommand.Items.Sum(i => i.Quantity * i.UnitPrice);

var orderPlacedEvent = new OrderPlaced(
    MessageId: Guid.NewGuid(),
    OrderId: orderId,
    CustomerId: customerId,
    TotalAmount: totalAmount,
    Items: placeOrderCommand.Items,
    PlacedAt: DateTimeOffset.UtcNow,
    CorrelationId: correlationId);  // Same correlation ID links command to event!

Console.WriteLine("Order placed! Publishing event: OrderPlaced");
Console.WriteLine($"  CorrelationId: {correlationId} (same!)");
Console.WriteLine($"  OrderId: {orderId}");

await producer.ProduceAsync(
    "events",
    orderId.ToString(),
    JsonSerializer.Serialize(orderPlacedEvent));

Console.WriteLine();

// Step 3 - Demonstrate Immutability
Console.WriteLine("--- Demonstrating Immutability ---");
Console.WriteLine();

var originalEvent = new OrderPlaced(
    MessageId: Guid.NewGuid(),
    OrderId: Guid.NewGuid(),
    CustomerId: Guid.NewGuid(),
    TotalAmount: 99.99m,
    Items: new List<OrderItem>(),
    PlacedAt: DateTimeOffset.UtcNow,
    CorrelationId: Guid.NewGuid());

Console.WriteLine("Attempting to modify a record...");

// This creates a NEW record with the modified value - original is unchanged
var modifiedEvent = originalEvent with { TotalAmount = 149.99m };

Console.WriteLine("Records are immutable - you get a new instance with 'with'");
Console.WriteLine($"  Original total: {originalEvent.TotalAmount}");
Console.WriteLine($"  Modified total: {modifiedEvent.TotalAmount} (new instance)");
Console.WriteLine();

// Step 4 - Demonstrate Correlation ID Tracing
Console.WriteLine("--- Demonstrating Correlation ID Tracing ---");
Console.WriteLine();

var traceCorrelationId = Guid.NewGuid();
Console.WriteLine($"Simulating order flow with CorrelationId: {traceCorrelationId}");

// In a real system, these events would be produced by different services
// but they all share the same CorrelationId for tracing
Console.WriteLine("  1. PlaceOrder command received");
Console.WriteLine("  2. OrderPlaced event published");
Console.WriteLine("  3. PaymentProcessed event published");
Console.WriteLine("  4. InventoryReserved event published");
Console.WriteLine("  All events share the same CorrelationId!");
Console.WriteLine();

// Create downstream events with the same correlation ID
var flowOrderId = Guid.NewGuid();

var paymentEvent = new PaymentProcessed(
    MessageId: Guid.NewGuid(),
    PaymentId: Guid.NewGuid(),
    OrderId: flowOrderId,
    Amount: 99.99m,
    ProcessedAt: DateTimeOffset.UtcNow,
    CorrelationId: traceCorrelationId);

var inventoryEvent = new InventoryReserved(
    MessageId: Guid.NewGuid(),
    ReservationId: Guid.NewGuid(),
    ProductId: Guid.NewGuid(),
    Quantity: 5,
    ReservedAt: DateTimeOffset.UtcNow,
    CorrelationId: traceCorrelationId);

await producer.ProduceAsync("events", flowOrderId.ToString(), JsonSerializer.Serialize(paymentEvent));
await producer.ProduceAsync("events", flowOrderId.ToString(), JsonSerializer.Serialize(inventoryEvent));

// Step 5 - Demonstrate Idempotent Processing
Console.WriteLine("--- Demonstrating Idempotent Processing ---");
Console.WriteLine();

// Simple in-memory deduplication (production systems would use a persistent store)
var processedOrders = new HashSet<string>();

void ProcessOrderIdempotently(string orderId)
{
    Console.Write($"Processing order {orderId}... ");

    // Check if we've already processed this order
    if (processedOrders.Contains(orderId))
    {
        Console.WriteLine("Already processed, skipped.");
        return;
    }

    // Process the order (idempotent - same result if called multiple times)
    processedOrders.Add(orderId);
    Console.WriteLine("Processed!");
}

// Simulate duplicate messages (common in distributed systems due to retries)
ProcessOrderIdempotently("order-123");
ProcessOrderIdempotently("order-123");  // Duplicate - should be skipped
ProcessOrderIdempotently("order-123");  // Another duplicate - should be skipped

Console.WriteLine("Idempotency prevents duplicate processing!");
Console.WriteLine();

// Step 6 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");

// Message type definitions - using records for immutability
// These are placed at the end of the file for top-level statements compatibility

// Command: A request for something to happen (imperative naming)
public record PlaceOrder(Guid CustomerId, List<OrderItem> Items, Guid CorrelationId);
public record OrderItem(Guid ProductId, int Quantity, decimal UnitPrice);

// Events: Facts about what happened (past tense naming)
public record OrderPlaced(
    Guid MessageId,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    List<OrderItem> Items,
    DateTimeOffset PlacedAt,
    Guid CorrelationId);

public record PaymentProcessed(
    Guid MessageId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    DateTimeOffset ProcessedAt,
    Guid CorrelationId);

public record InventoryReserved(
    Guid MessageId,
    Guid ReservationId,
    Guid ProductId,
    int Quantity,
    DateTimeOffset ReservedAt,
    Guid CorrelationId);
