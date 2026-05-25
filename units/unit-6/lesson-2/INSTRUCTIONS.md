# Lesson 18.2: Instructions

Follow these step-by-step instructions to implement seek operations.

## Step 1: Set Up the Environment

Create a producer and consumer, then produce some messages with timestamps:

```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Produce messages with delays to create distinct timestamps
for (int i = 1; i <= 10; i++)
{
    await producer.ProduceAsync(topic, $"key-{i}", $"Message-{i}");
    await Task.Delay(100); // Create timestamp gaps
}
```

## Step 2: Seek to Beginning

Use `Seek()` with offset 0 to start from the beginning:

```csharp
// Assign the partition manually (no consumer group)
consumer.Assign(topic, partition: 0, offset: 0);

// Or use Seek after assignment:
consumer.Seek(topic, partition: 0, offset: 0);

// Now consume from the beginning
var result = await consumer.ConsumeAsync();
Console.WriteLine($"First message: {result.Value} at offset {result.Offset}");
```

## Step 3: Seek to Specific Offset

Jump to a specific offset in the partition:

```csharp
// Jump to offset 5 (skip first 5 messages)
consumer.Seek(topic, partition: 0, offset: 5);

var result = await consumer.ConsumeAsync();
Console.WriteLine($"Message at offset 5: {result.Value}");
```

## Step 4: Seek to End

To start consuming from the latest messages:

```csharp
// For standalone consumers, use native client to get latest offset
// In this lesson, we'll demonstrate by producing more messages
// after seeking to the known end position

// Get current position
var currentPos = consumer.Position(topic, partition: 0);
Console.WriteLine($"Current position: {currentPos}");

// Seek to end (latest offset)
consumer.Seek(topic, 0, latestOffset);
```

## Step 5: Seek to Timestamp

Seek to messages from a specific point in time:

```csharp
// Record timestamp before producing
var startTime = DateTimeOffset.UtcNow;

// Produce messages
await producer.ProduceAsync(topic, "key-1", "Before");
await Task.Delay(1000);

var seekTime = DateTimeOffset.UtcNow; // Mark this time
await Task.Delay(100);

await producer.ProduceAsync(topic, "key-2", "After-1");
await producer.ProduceAsync(topic, "key-3", "After-2");

// Seek to the timestamp - will find first message at or after seekTime
// Note: This requires using the messaging operations to get offset for timestamp
```

## Step 6: Handle Edge Cases

Be aware of edge cases when seeking:

```csharp
// Seeking beyond end - will wait for new messages
consumer.Seek(topic, 0, latestOffset + 100);
// ConsumeAsync will return null until messages arrive

// Seeking before beginning - starts at first available
consumer.Seek(topic, 0, -10); // Will start at offset 0

// Seeking in empty partition
// ConsumeAsync returns null
```

## Step 7: Practical Pattern - Skip Bad Messages

Use seek to skip messages that cause errors:

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    var currentOffset = consumer.Position(topic, 0);

    try
    {
        var result = await consumer.ConsumeAsync(cancellationToken);
        if (result == null) continue;

        ProcessMessage(result); // May throw
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error at offset {currentOffset}: {ex.Message}");
        // Skip the bad message
        consumer.Seek(topic, 0, currentOffset + 1);
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Seek to beginning
- Seek to specific offset
- Seek to end
- Position tracking
- Skip bad message pattern

## Sample Output

```
=== Surgewave Bootcamp: Lesson 18.2 - Seek Operations ===

Surgewave broker started on port 54321

Producing 10 test messages...
Produced Message-1 at offset 0
Produced Message-2 at offset 1
...
Produced Message-10 at offset 9

--- Seek to Beginning ---
Seeking to offset 0...
Consumed: Message-1 at offset 0

--- Seek to Specific Offset ---
Seeking to offset 5...
Consumed: Message-6 at offset 5
Consumed: Message-7 at offset 6

--- Current Position ---
Current position: offset 7

--- Seek to End ---
Seeking to latest offset (10)...
Producing new message...
Consumed: NewMessage at offset 10

--- Skip Bad Message Pattern ---
Processing offset 0: Message-1
Processing offset 1: Message-2
Error at offset 2: Simulated error
Skipping to offset 3...
Processing offset 3: Message-4
...

Lesson complete!
```

## Knowledge Check

1. What happens if you seek to an offset that doesn't exist?
2. How does seek-to-timestamp work if there's no exact match?
3. Why might you need to seek to beginning in production?
4. What's the difference between `Seek()` and `Assign()` with an offset?
5. How can seek be used for disaster recovery?

---

**Next:** [Lesson 18.3: Pause & Resume](../lesson-3/README.md)
