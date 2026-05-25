# Lesson 1.2: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see familiar code from Lesson 1.1 for starting the broker and connecting, plus new TODOs for producing messages.

---

## Step 2: Add Using Statements

At the top of the file, you need the same namespaces as before, plus the client namespace:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
```

The `Kuestenlogik.Surgewave.Client` namespace provides:
- `SurgewaveProducer<TKey, TValue>` - The strongly-typed producer
- `ProduceResult` - The result of a produce operation

---

## Step 3: Create a Producer

After connecting to the broker, create a producer:

```csharp
// Step 2 - Create a producer
Console.WriteLine("Creating producer...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("Producer ready!");
Console.WriteLine();
```

**What this does:**
- `SurgewaveProducer<string, string>` creates a producer with string keys and string values
- `BootstrapServers` tells the producer where to connect
- `await using` ensures the producer is properly disposed

---

## Step 4: Produce Messages

Now produce some messages:

```csharp
// Step 3 - Produce messages
Console.WriteLine("Producing messages...");

var messages = new[]
{
    ("user-1", "Hello Surgewave!"),
    ("user-2", "Surgewave is fast!"),
    ("user-1", "And easy to use!")
};

foreach (var (key, value) in messages)
{
    var result = await producer.ProduceAsync("greetings", key, value);
    Console.WriteLine($"  Message: key='{key}', value='{value}' -> Offset {result.Offset}");
}

Console.WriteLine();
Console.WriteLine($"Produced {messages.Length} messages to 'greetings' topic");
Console.WriteLine();
```

**What this does:**
- `ProduceAsync` sends a message to the specified topic
- The key is used for partitioning (same key = same partition)
- The result contains the offset where the message was stored
- Each message gets a sequential offset (0, 1, 2, ...)

---

## Step 5: Understanding Keys and Partitioning

Keys serve two important purposes:

1. **Partitioning**: Messages with the same key go to the same partition
2. **Semantics**: Keys often represent entity IDs (user ID, order ID, etc.)

In our example:
- Messages with key `user-1` will be processed in order
- This ensures all events for a user are handled sequentially

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.2 - Producing Messages ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Create a producer
Console.WriteLine("Creating producer...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("Producer ready!");
Console.WriteLine();

// Step 3 - Produce messages
Console.WriteLine("Producing messages...");

var messages = new[]
{
    ("user-1", "Hello Surgewave!"),
    ("user-2", "Surgewave is fast!"),
    ("user-1", "And easy to use!")
};

foreach (var (key, value) in messages)
{
    var result = await producer.ProduceAsync("greetings", key, value);
    Console.WriteLine($"  Message: key='{key}', value='{value}' -> Offset {result.Offset}");
}

Console.WriteLine();
Console.WriteLine($"Produced {messages.Length} messages to 'greetings' topic");
Console.WriteLine();

// Step 4 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
```

---

## Run and Verify

```bash
cd start
dotnet run
```

Expected output:

```
=== Surgewave Bootcamp: Lesson 1.2 - Producing Messages ===

Starting embedded Surgewave broker...
Broker started on localhost:9092

Creating producer...
Producer ready!

Producing messages...
  Message: key='user-1', value='Hello Surgewave!' -> Offset 0
  Message: key='user-2', value='Surgewave is fast!' -> Offset 1
  Message: key='user-1', value='And easy to use!' -> Offset 2

Produced 3 messages to 'greetings' topic

Shutting down...
Goodbye!
```

---

## Experiment (Optional)

Try these variations to deepen your understanding:

### 1. Produce JSON Objects

```csharp
public record UserMessage(string UserId, string Text, DateTime Timestamp);

await using var producer = new SurgewaveProducer<string, UserMessage>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    // JSON serializer is automatically selected for complex types
});

var msg = new UserMessage("user-1", "Hello!", DateTime.UtcNow);
await producer.ProduceAsync("user-messages", msg.UserId, msg);
```

### 2. Use Delivery Handlers

```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.DeliveryHandler = report =>
    {
        if (report.Success)
            Console.WriteLine($"Delivered to offset {report.Result!.Offset}");
        else
            Console.WriteLine($"Delivery failed: {report.Error}");
    };
});
```

### 3. Batch Multiple Messages

Produce many messages quickly:

```csharp
for (int i = 0; i < 1000; i++)
{
    await producer.ProduceAsync("high-volume", $"key-{i % 10}", $"Message {i}");
}
await producer.FlushAsync(); // Ensure all messages are sent
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What happens if you produce to a topic that doesn't exist?
2. Why would you use different keys for different messages?
3. What does the offset in `ProduceResult` tell you?
4. How does Surgewave decide which serializer to use?

---

**Ready for more?** → [Lesson 1.3: Consuming Messages](../lesson-3/README.md)
