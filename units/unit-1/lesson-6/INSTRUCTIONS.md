# Lesson 1.6: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand Events vs Commands

The most important distinction in event-driven systems:

| Events | Commands |
|--------|----------|
| Facts about the past | Requests for the future |
| Cannot be rejected | Can be accepted or rejected |
| Multiple subscribers | Single handler |
| `OrderPlaced` | `PlaceOrder` |

---

## Step 2: Add Using Statements

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using System.Text.Json;
```

---

## Step 3: Define Immutable Message Types

Use C# records for message definitions:

```csharp
// Command: A request for something to happen
public record PlaceOrder(
    Guid CustomerId,
    List<OrderItem> Items,
    Guid CorrelationId);

public record OrderItem(Guid ProductId, int Quantity, decimal UnitPrice);

// Event: A fact about something that happened
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
```

**Key points:**
- Use `record` for automatic immutability
- Include `MessageId` for deduplication
- Include `CorrelationId` for tracing
- Include timestamps for ordering/debugging
- Events use past tense, commands use imperative

---

## Step 4: Start the Broker

```csharp
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();
```

---

## Step 5: Demonstrate Events vs Commands

Show the flow from command to event:

```csharp
Console.WriteLine("--- Demonstrating Events vs Commands ---");
Console.WriteLine();

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Generate a correlation ID that will follow this request
var correlationId = Guid.NewGuid();
var customerId = Guid.NewGuid();

// Create a command (request for action)
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

// Simulate command handler creating an event
var orderId = Guid.NewGuid();
var totalAmount = placeOrderCommand.Items.Sum(i => i.Quantity * i.UnitPrice);

var orderPlacedEvent = new OrderPlaced(
    MessageId: Guid.NewGuid(),
    OrderId: orderId,
    CustomerId: customerId,
    TotalAmount: totalAmount,
    Items: placeOrderCommand.Items,
    PlacedAt: DateTimeOffset.UtcNow,
    CorrelationId: correlationId);  // Same correlation ID!

Console.WriteLine("Order placed! Publishing event: OrderPlaced");
Console.WriteLine($"  CorrelationId: {correlationId} (same!)");
Console.WriteLine($"  OrderId: {orderId}");

await producer.ProduceAsync(
    "events",
    orderId.ToString(),
    JsonSerializer.Serialize(orderPlacedEvent));

Console.WriteLine();
```

---

## Step 6: Demonstrate Immutability

Show how records enforce immutability:

```csharp
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

// This creates a NEW record, doesn't modify the original
var modifiedEvent = originalEvent with { TotalAmount = 149.99m };

Console.WriteLine("Records are immutable - you get a new instance with 'with'");
Console.WriteLine($"  Original total: {originalEvent.TotalAmount}");
Console.WriteLine($"  Modified total: {modifiedEvent.TotalAmount} (new instance)");
Console.WriteLine();
```

---

## Step 7: Demonstrate Correlation ID Tracing

Show how correlation IDs connect related messages:

```csharp
Console.WriteLine("--- Demonstrating Correlation ID Tracing ---");
Console.WriteLine();

var traceCorrelationId = Guid.NewGuid();
Console.WriteLine($"Simulating order flow with CorrelationId: {traceCorrelationId}");

// Simulate a complete flow with the same correlation ID
Console.WriteLine("  1. PlaceOrder command received");
Console.WriteLine("  2. OrderPlaced event published");
Console.WriteLine("  3. PaymentProcessed event published");
Console.WriteLine("  4. InventoryReserved event published");
Console.WriteLine("  All events share the same CorrelationId!");
Console.WriteLine();

// Produce events with the same correlation ID
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
```

---

## Step 8: Demonstrate Idempotency

Show basic idempotent processing pattern:

```csharp
Console.WriteLine("--- Demonstrating Idempotent Processing ---");
Console.WriteLine();

// Simple in-memory deduplication (production would use a persistent store)
var processedOrders = new HashSet<string>();

void ProcessOrderIdempotently(string orderId)
{
    Console.Write($"Processing order {orderId}... ");

    if (processedOrders.Contains(orderId))
    {
        Console.WriteLine("Already processed, skipped.");
        return;
    }

    // Process the order
    processedOrders.Add(orderId);
    Console.WriteLine("Processed!");
}

// Simulate duplicate messages (common in distributed systems)
ProcessOrderIdempotently("order-123");
ProcessOrderIdempotently("order-123");  // Duplicate!
ProcessOrderIdempotently("order-123");  // Another duplicate!

Console.WriteLine("Idempotency prevents duplicate processing!");
Console.WriteLine();
```

---

## Complete Solution

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using System.Text.Json;

// Message type definitions
public record PlaceOrder(Guid CustomerId, List<OrderItem> Items, Guid CorrelationId);
public record OrderItem(Guid ProductId, int Quantity, decimal UnitPrice);
public record OrderPlaced(Guid MessageId, Guid OrderId, Guid CustomerId, decimal TotalAmount, List<OrderItem> Items, DateTimeOffset PlacedAt, Guid CorrelationId);
public record PaymentProcessed(Guid MessageId, Guid PaymentId, Guid OrderId, decimal Amount, DateTimeOffset ProcessedAt, Guid CorrelationId);
public record InventoryReserved(Guid MessageId, Guid ReservationId, Guid ProductId, int Quantity, DateTimeOffset ReservedAt, Guid CorrelationId);

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.6 - Message Best Practices ===");
Console.WriteLine();

// Step 1 - Start the broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
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

var correlationId = Guid.NewGuid();
var customerId = Guid.NewGuid();

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

await producer.ProduceAsync("commands", customerId.ToString(), JsonSerializer.Serialize(placeOrderCommand));

Console.WriteLine();

var orderId = Guid.NewGuid();
var totalAmount = placeOrderCommand.Items.Sum(i => i.Quantity * i.UnitPrice);

var orderPlacedEvent = new OrderPlaced(
    MessageId: Guid.NewGuid(),
    OrderId: orderId,
    CustomerId: customerId,
    TotalAmount: totalAmount,
    Items: placeOrderCommand.Items,
    PlacedAt: DateTimeOffset.UtcNow,
    CorrelationId: correlationId);

Console.WriteLine("Order placed! Publishing event: OrderPlaced");
Console.WriteLine($"  CorrelationId: {correlationId} (same!)");
Console.WriteLine($"  OrderId: {orderId}");

await producer.ProduceAsync("events", orderId.ToString(), JsonSerializer.Serialize(orderPlacedEvent));

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

Console.WriteLine("  1. PlaceOrder command received");
Console.WriteLine("  2. OrderPlaced event published");
Console.WriteLine("  3. PaymentProcessed event published");
Console.WriteLine("  4. InventoryReserved event published");
Console.WriteLine("  All events share the same CorrelationId!");
Console.WriteLine();

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

// Step 5 - Demonstrate Idempotency
Console.WriteLine("--- Demonstrating Idempotent Processing ---");
Console.WriteLine();

var processedOrders = new HashSet<string>();

void ProcessOrderIdempotently(string orderId)
{
    Console.Write($"Processing order {orderId}... ");

    if (processedOrders.Contains(orderId))
    {
        Console.WriteLine("Already processed, skipped.");
        return;
    }

    processedOrders.Add(orderId);
    Console.WriteLine("Processed!");
}

ProcessOrderIdempotently("order-123");
ProcessOrderIdempotently("order-123");
ProcessOrderIdempotently("order-123");

Console.WriteLine("Idempotency prevents duplicate processing!");
Console.WriteLine();

// Step 6 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
```

---

## Run and Verify

```bash
cd start
dotnet run
```

---

## Experiment (Optional)

### 1. Add Timestamp to Commands

Commands can also have timestamps:

```csharp
public record PlaceOrder(
    Guid CustomerId,
    List<OrderItem> Items,
    DateTimeOffset RequestedAt,  // When the command was issued
    Guid CorrelationId);
```

### 2. Add Causation IDs

Track which message caused another:

```csharp
public record PaymentProcessed(
    Guid MessageId,
    Guid OrderId,
    Guid CorrelationId,
    Guid CausationId);  // The MessageId of the event that caused this
```

### 3. Create a Message Base Type

```csharp
public abstract record Message
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record OrderPlaced : Message
{
    public Guid OrderId { get; init; }
    public decimal TotalAmount { get; init; }
}
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What's the difference between an event and a command?
2. Why should messages be immutable?
3. What is a correlation ID and why is it important?
4. What does idempotency mean and why do we need it?
5. What should and shouldn't be included in message payloads?

---

## Best Practices Summary

1. **Use records** for immutable messages
2. **Events are past tense** (`OrderPlaced`), **commands are imperative** (`PlaceOrder`)
3. **Include correlation IDs** for tracing across services
4. **Include unique message IDs** for idempotency
5. **Include timestamps** for debugging and ordering
6. **Include what consumers need**, not everything you have
7. **Design for multiple consumers** - events are public contracts

---

## Unit 1 Complete!

Congratulations on completing Unit 1 of the Surgewave Bootcamp! You now understand:

- **Embedded Broker**: Running Surgewave in your application
- **Producing**: Sending messages with keys and values
- **Consuming**: Reading messages with offset tracking
- **Consumer Groups**: Coordinated consumption
- **Partitioning**: Parallel processing with ordering guarantees
- **Best Practices**: Events, commands, correlation IDs, and immutability

**Continue to Unit 3** -> [Testing Surgewave](../../unit-3/README.md)
