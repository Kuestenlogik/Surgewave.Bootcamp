# Lesson 3.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Create Idempotent Producer

```csharp
var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.EnableIdempotence = true;
    opt.Acks = Acks.All;
    opt.MaxInFlight = 5; // Safe with idempotence
});
```

## Step 2: Configure Consumer for Exactly-Once

```csharp
var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "exactly-once-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    opt.EnableAutoCommit = false; // Manual commit
    opt.IsolationLevel = IsolationLevel.ReadCommitted;
});
```

## Step 3: Implement Exactly-Once Processing Loop

```csharp
await producer.InitTransactionsAsync();

while (true)
{
    var batch = await consumer.ConsumeBatchAsync(
        maxMessages: 100,
        timeout: TimeSpan.FromSeconds(1));

    if (batch.Count == 0) break;

    await producer.BeginTransactionAsync();

    try
    {
        foreach (var message in batch)
        {
            // Transform
            var output = Transform(message.Value);

            // Produce
            await producer.ProduceAsync("output", message.Key, output);
        }

        // Commit offsets as part of transaction
        await producer.SendOffsetsToTransactionAsync(
            consumer.GetCurrentOffsets(),
            consumer.GroupId);

        await producer.CommitTransactionAsync();
        Console.WriteLine($"Processed {batch.Count} messages exactly-once");
    }
    catch
    {
        await producer.AbortTransactionAsync();
        throw;
    }
}
```

## Step 4: Verify No Duplicates

```csharp
// Count messages in output topic
var outputConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.IsolationLevel = IsolationLevel.ReadCommitted;
});

outputConsumer.Subscribe("output");

var keys = new HashSet<string>();
int duplicates = 0;

while (true)
{
    var msg = await outputConsumer.ConsumeAsync(timeout);
    if (msg == null) break;

    if (!keys.Add(msg.Key))
        duplicates++;
}

Console.WriteLine($"Total: {keys.Count}, Duplicates: {duplicates}");
```

## Step 5: Application-Level Idempotency

For cases where broker-level exactly-once isn't sufficient:

```csharp
public class IdempotentProcessor
{
    private readonly HashSet<string> _processedIds = new();

    public async Task ProcessAsync(ConsumeResult<string, string> message)
    {
        // Check if already processed
        var messageId = message.Headers.GetValueOrDefault("x-message-id")
            ?? message.Key;

        if (!_processedIds.Add(messageId))
        {
            Console.WriteLine($"Skipping duplicate: {messageId}");
            return;
        }

        // Process message
        await DoProcess(message);
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What's the difference between idempotence and exactly-once?
2. Why disable auto-commit for exactly-once processing?
3. How does IsolationLevel.ReadCommitted help?

---

**Next:** → [Lesson 3.6: Compacted Topics](../lesson-6/README.md)
