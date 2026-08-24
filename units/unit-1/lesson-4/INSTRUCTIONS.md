# Lesson 1.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand Partitions

Partitions provide:
1. **Parallelism**: Multiple consumers can read simultaneously
2. **Ordering**: Messages with the same key maintain order
3. **Scalability**: Add partitions to increase throughput

---

## Step 2: Add Using Statements

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Native;
```

Note: We add `Kuestenlogik.Surgewave.Client.Consumer` for `AutoOffsetReset` and `Kuestenlogik.Surgewave.Client.Native` for direct topic management.

---

## Step 3: Create a Multi-Partition Topic

After starting the broker, create a topic with 3 partitions:

```csharp
// Step 2 - Create a topic with multiple partitions
Console.WriteLine("Creating topic 'user-events' with 3 partitions...");

await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

await client.Topics.CreateAsync("user-events", partitions: 3);

Console.WriteLine("Topic created!");
Console.WriteLine();
```

**What this does:**
- Creates a direct native client connection
- Creates a topic named "user-events" with 3 partitions
- Each partition can be consumed independently

---

## Step 4: Produce Messages with Keys

Produce events for different users - same user = same partition:

```csharp
// Step 3 - Produce messages with keys
Console.WriteLine("Producing user events...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var events = new[]
{
    ("user-1", "login"),
    ("user-2", "login"),
    ("user-3", "login"),
    ("user-1", "view-product"),
    ("user-2", "add-to-cart"),
    ("user-1", "add-to-cart"),
    ("user-3", "view-product"),
    ("user-1", "checkout"),
    ("user-2", "checkout"),
    ("user-3", "add-to-cart"),
};

foreach (var (userId, eventType) in events)
{
    var result = await producer.ProduceAsync("user-events", userId, eventType);
    Console.WriteLine($"  {userId}: {eventType} -> Partition {result.Partition}");
}

Console.WriteLine();
```

**Observe:**
- All events for the same user go to the same partition
- This preserves the order of events per user

---

## Step 5: List Topic Details

Check the topic we created:

```csharp
// Step 4 - List topic details
Console.WriteLine("Listing topic partitions...");

var topics = await client.Topics.ListAsync();
foreach (var topic in topics)
{
    Console.WriteLine($"  Topic: {topic.Name}");
    Console.WriteLine($"  Partitions: {topic.PartitionCount}");
}

Console.WriteLine();
```

---

## Step 6: Consume and Track Partition Distribution

Consume messages and track which partition each comes from:

```csharp
// Step 5 - Consume from all partitions
Console.WriteLine("Consuming from all partitions...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("user-events");

var partitionCounts = new Dictionary<int, int>();
int totalConsumed = 0;
const int expectedMessages = 10;

while (totalConsumed < expectedMessages)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  [P{result.Partition}] {result.Key}: {result.Value}");

        if (!partitionCounts.ContainsKey(result.Partition))
            partitionCounts[result.Partition] = 0;
        partitionCounts[result.Partition]++;

        totalConsumed++;
    }
}

Console.WriteLine();
Console.WriteLine("Message distribution by partition:");
foreach (var (partition, count) in partitionCounts.OrderBy(p => p.Key))
{
    Console.WriteLine($"  Partition {partition}: {count} messages");
}

Console.WriteLine();
```

---

## Complete Solution

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.5 - Partitioning ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(false)  // We'll create the topic manually
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Create a topic with multiple partitions
Console.WriteLine("Creating topic 'user-events' with 3 partitions...");

await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

await client.Topics.CreateAsync("user-events", partitions: 3);

Console.WriteLine("Topic created!");
Console.WriteLine();

// Step 3 - Produce messages with keys
Console.WriteLine("Producing user events...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var events = new[]
{
    ("user-1", "login"),
    ("user-2", "login"),
    ("user-3", "login"),
    ("user-1", "view-product"),
    ("user-2", "add-to-cart"),
    ("user-1", "add-to-cart"),
    ("user-3", "view-product"),
    ("user-1", "checkout"),
    ("user-2", "checkout"),
    ("user-3", "add-to-cart"),
};

foreach (var (userId, eventType) in events)
{
    var result = await producer.ProduceAsync("user-events", userId, eventType);
    Console.WriteLine($"  {userId}: {eventType} -> Partition {result.Partition}");
}

Console.WriteLine();

// Step 4 - List topic details
Console.WriteLine("Listing topic partitions...");

var topics = await client.Topics.ListAsync();
foreach (var topic in topics)
{
    Console.WriteLine($"  Topic: {topic.Name}");
    Console.WriteLine($"  Partitions: {topic.PartitionCount}");
}

Console.WriteLine();

// Step 5 - Consume from all partitions
Console.WriteLine("Consuming from all partitions...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("user-events");

var partitionCounts = new Dictionary<int, int>();
int totalConsumed = 0;
const int expectedMessages = 10;

while (totalConsumed < expectedMessages)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  [P{result.Partition}] {result.Key}: {result.Value}");

        if (!partitionCounts.ContainsKey(result.Partition))
            partitionCounts[result.Partition] = 0;
        partitionCounts[result.Partition]++;

        totalConsumed++;
    }
}

Console.WriteLine();
Console.WriteLine("Message distribution by partition:");
foreach (var (partition, count) in partitionCounts.OrderBy(p => p.Key))
{
    Console.WriteLine($"  Partition {partition}: {count} messages");
}

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

### 1. Produce to a Specific Partition

```csharp
// Override the default key-based routing
var result = await producer.ProduceAsync("user-events", partition: 0, "any-key", "forced-to-partition-0");
```

### 2. Create More Partitions

```csharp
// Create a high-throughput topic
await client.Topics.CreateAsync("high-volume", partitions: 12);
```

### 3. Check Ordering Within a Partition

Notice how all events for the same user appear in order when consumed from their partition.

### 4. Consume from a Single Partition

```csharp
// Instead of Subscribe, use Assign to consume only partition 0
consumer.Assign("user-events", partition: 0, offset: 0);
```

---

## Knowledge Check

Before completing Unit 1, make sure you can answer:

1. Why do messages with the same key go to the same partition?
2. What happens if you have 3 partitions but only 2 consumers in a group?
3. Can you decrease the number of partitions in a topic?
4. How does partitioning help with scalability?

---

## Unit 1 Complete!

You've finished Unit 1 of the Surgewave Bootcamp. You now have a solid foundation in:

- **Embedded Broker**: Running Surgewave in your application
- **Producing**: Sending messages with keys and values
- **Consuming**: Reading messages with offset tracking
- **Consumer Groups**: Coordinated consumption
- **Partitioning**: Parallel processing with ordering guarantees

**Continue to Unit 3** → [Testing Surgewave](../../unit-3/README.md)
