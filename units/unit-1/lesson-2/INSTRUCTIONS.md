# Lesson 1.3: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Understand the Starting Code

Open `start/Program.cs`. You'll see code that starts a broker, produces some messages, and has TODOs for consuming them.

---

## Step 2: Add Using Statements

At the top of the file, add the required namespaces:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
```

---

## Step 3: Create a Consumer

After producing messages, create a consumer:

```csharp
// Step 3 - Create a consumer
Console.WriteLine("Creating consumer...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;  // Start from beginning
});

Console.WriteLine("Consumer ready!");
Console.WriteLine();
```

**What this does:**
- `SurgewaveConsumer<string, string>` creates a consumer expecting string keys and values
- `AutoOffsetReset.Earliest` ensures we read from the beginning of the topic
- Without this, we'd only see new messages

---

## Step 4: Subscribe to a Topic

Subscribe to the topic we produced messages to:

```csharp
// Subscribe to the topic
consumer.Subscribe("greetings");
```

---

## Step 5: Consume Messages

Read messages one at a time:

```csharp
// Step 4 - Consume messages
Console.WriteLine("Consuming messages...");

int messageCount = 0;
const int expectedMessages = 3;

while (messageCount < expectedMessages)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  Received [{result.Offset}]: Key='{result.Key}', Value='{result.Value}'");
        messageCount++;
    }
}

Console.WriteLine();
Console.WriteLine($"Consumed {messageCount} messages from 'greetings' topic");
Console.WriteLine();
```

**What this does:**
- `ConsumeAsync()` fetches the next message or returns null if none available
- The timeout prevents blocking forever
- We track how many messages we've received

---

## Step 6: Understanding Offsets

Each message has a unique offset within its partition:
- Offset 0 is the first message
- Offsets are sequential within a partition
- The consumer tracks its position automatically

You can also manually seek:

```csharp
consumer.Seek("greetings", partition: 0, offset: 0);  // Go back to start
```

---

## Complete Solution

Your completed `Program.cs` should look like this:

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.3 - Consuming Messages ===");
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

// Step 2 - First, produce some messages
Console.WriteLine("Producing messages first...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var messages = new[]
{
    ("user-1", "Hello Surgewave!"),
    ("user-2", "Surgewave is fast!"),
    ("user-1", "And easy to use!")
};

foreach (var (key, value) in messages)
{
    await producer.ProduceAsync("greetings", key, value);
    Console.WriteLine($"  Produced: {value}");
}

Console.WriteLine();

// Step 3 - Create a consumer
Console.WriteLine("Creating consumer...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("greetings");

Console.WriteLine("Consumer ready!");
Console.WriteLine();

// Step 4 - Consume messages
Console.WriteLine("Consuming messages...");

int messageCount = 0;
const int expectedMessages = 3;

while (messageCount < expectedMessages)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  Received [{result.Offset}]: Key='{result.Key}', Value='{result.Value}'");
        messageCount++;
    }
}

Console.WriteLine();
Console.WriteLine($"Consumed {messageCount} messages from 'greetings' topic");
Console.WriteLine();

// Step 5 - Clean shutdown
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
=== Surgewave Bootcamp: Lesson 1.3 - Consuming Messages ===

Starting embedded Surgewave broker...
Broker started on localhost:9092

Producing messages first...
  Produced: Hello Surgewave!
  Produced: Surgewave is fast!
  Produced: And easy to use!

Creating consumer...
Consumer ready!

Consuming messages...
  Received [0]: Key='user-1', Value='Hello Surgewave!'
  Received [1]: Key='user-2', Value='Surgewave is fast!'
  Received [2]: Key='user-1', Value='And easy to use!'

Consumed 3 messages from 'greetings' topic

Shutting down...
Goodbye!
```

---

## Experiment (Optional)

### 1. Use Handler-Based Consumption

Instead of a loop, use event handlers:

```csharp
consumer
    .OnMessage(async (msg, ct) =>
    {
        Console.WriteLine($"Received: {msg.Value}");
    });

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await consumer.ConsumeLoopAsync(cts.Token);
}
catch (OperationCanceledException) { }
```

### 2. Consume from a Specific Offset

```csharp
// Don't subscribe, instead assign manually
consumer.Assign("greetings", partition: 0, offset: 1);  // Skip first message

var result = await consumer.ConsumeAsync();
// This will return the second message (offset 1)
```

### 3. Check Available Messages

```csharp
// Get the lag (how many messages we haven't read yet)
var lag = await consumer.GetLagAsync("greetings", partition: 0);
Console.WriteLine($"Messages available: {lag}");
```

---

## Knowledge Check

Before moving on, make sure you can answer:

1. What's the difference between `Subscribe()` and `Assign()`?
2. What does `AutoOffsetReset.Earliest` do?
3. What happens if you call `ConsumeAsync()` and there are no messages?
4. How does the consumer track which messages it has read?

---

**Ready for more?** → [Lesson 1.4: Consumer Groups](../lesson-4/README.md)
