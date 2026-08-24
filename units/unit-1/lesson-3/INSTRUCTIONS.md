# Lesson 1.4: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand Consumer Groups

Consumer groups enable:
1. **Load balancing**: Multiple consumers share the work
2. **Fault tolerance**: If one fails, others take over
3. **Progress tracking**: Committed offsets persist across restarts

---

## Step 2: Add Using Statements

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
```

---

## Step 3: Create a Consumer with Group ID

The key difference from Lesson 1.3 is setting `GroupId`:

```csharp
// Step 3 - Create a consumer with group ID
Console.WriteLine("Creating consumer with group 'order-processors'...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "order-processors";  // Consumer group name
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.EnableAutoCommit = false;  // We'll commit manually
});

// Handle partition assignment events
consumer.PartitionsAssigned += (sender, args) =>
{
    Console.WriteLine($"Partitions assigned: [{string.Join(", ", args.Partitions)}]");
};

consumer.PartitionsRevoked += (sender, args) =>
{
    Console.WriteLine($"Partitions revoked: [{string.Join(", ", args.Partitions)}]");
};
```

**What this does:**
- `GroupId` identifies this consumer as part of a group
- `EnableAutoCommit = false` lets us control when offsets are committed
- Event handlers let us see partition assignments

---

## Step 4: Join the Group with SubscribeAsync

Use `SubscribeAsync` to join the group immediately:

```csharp
Console.WriteLine("Joining consumer group...");
await consumer.SubscribeAsync(default, "orders");
Console.WriteLine("Consumer ready!");
Console.WriteLine();
```

**What happens:**
1. Consumer contacts the broker
2. Broker assigns partitions based on group membership
3. Consumer fetches any committed offsets
4. `PartitionsAssigned` event fires

---

## Step 5: Process Messages

Consume and process messages:

```csharp
// Step 4 - Process messages
Console.WriteLine("Processing orders...");

int processedCount = 0;
const int expectedOrders = 10;

while (processedCount < expectedOrders)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  Order {processedCount}: Processing {result.Value}");
        processedCount++;
    }
}

Console.WriteLine();
```

---

## Step 6: Commit Offsets

After processing, commit the offsets:

```csharp
// Step 5 - Commit offsets
Console.WriteLine("Committing offsets...");
await consumer.CommitAsync();
Console.WriteLine("Offsets committed successfully!");
Console.WriteLine();
Console.WriteLine($"Processed {processedCount} orders");
```

**Why commit?**
- Offsets mark your progress
- On restart, consumption resumes from the committed offset
- Without commits, you'd re-process all messages

---

## Complete Solution

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.4 - Consumer Groups ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Produce some orders
Console.WriteLine("Producing messages to 'orders' topic...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

for (int i = 0; i < 10; i++)
{
    var orderId = $"ORD-{i}";
    var orderValue = $"order {orderId} for ${(i + 1) * 10}.00";
    await producer.ProduceAsync("orders", orderId, orderValue);
}

Console.WriteLine("  Produced 10 orders");
Console.WriteLine();

// Step 3 - Create a consumer with group ID
Console.WriteLine("Creating consumer with group 'order-processors'...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "order-processors";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.EnableAutoCommit = false;
});

consumer.PartitionsAssigned += (sender, args) =>
{
    Console.WriteLine($"Partitions assigned: [{string.Join(", ", args.Partitions)}]");
};

consumer.PartitionsRevoked += (sender, args) =>
{
    Console.WriteLine($"Partitions revoked: [{string.Join(", ", args.Partitions)}]");
};

Console.WriteLine("Joining consumer group...");
await consumer.SubscribeAsync(default, "orders");
Console.WriteLine("Consumer ready!");
Console.WriteLine();

// Step 4 - Process messages
Console.WriteLine("Processing orders...");

int processedCount = 0;
const int expectedOrders = 10;

while (processedCount < expectedOrders)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  Order {processedCount}: Processing {result.Value}");
        processedCount++;
    }
}

Console.WriteLine();

// Step 5 - Commit offsets
Console.WriteLine("Committing offsets...");
await consumer.CommitAsync();
Console.WriteLine("Offsets committed successfully!");
Console.WriteLine();
Console.WriteLine($"Processed {processedCount} orders");
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

### 1. Restart and Verify Offset Commit

Run the program twice. The second time should show no messages (all already committed):

```csharp
// Change WithStorageEngine to persist data:
.WithStorageEngine(StorageEngines.File)
.WithDataDirectory("./surgewave-data")
```

### 2. Commit After Each Message

```csharp
if (result != null)
{
    Console.WriteLine($"Processing: {result.Value}");
    await consumer.CommitAsync(result);  // Commit each message
}
```

### 3. Multiple Consumer Instances

In a real application, you could run multiple instances of this program. Each would get different partitions assigned (if the topic had multiple partitions).

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What happens if you don't commit offsets and restart the consumer?
2. Why would you use manual commits instead of auto-commit?
3. What happens when a consumer leaves a group?
4. Can two consumers in the same group read the same partition?

---

**Ready for more?** → [Lesson 1.5: Partitioning](../lesson-5/README.md)
