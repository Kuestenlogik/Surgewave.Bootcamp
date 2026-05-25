# Lesson 3.6: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Create Native Client for Topic Management

Create a native client to use the Surgewave topic and cluster APIs:

```csharp
await using var nativeClient = new SurgewaveNativeClient("localhost", runtime.Port);
await nativeClient.ConnectAsync();
```

## Step 2: Create Compacted Topic

Use the fluent builder to create a topic with compaction enabled:

```csharp
await nativeClient.Topics.Create("user-profiles")
    .WithPartitions(1)
    .WithCompaction()
    .ExecuteAsync();
Console.WriteLine("Created compacted topic: user-profiles");
```

## Step 3: Create Producer and Consumer

```csharp
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "compaction-demo";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
```

## Step 4: Produce Multiple Updates Per Key

Simulate user profile updates with multiple values per key:

```csharp
var users = new[] { "user-1", "user-2", "user-3" };

// First round of updates
foreach (var userId in users)
{
    await producer.ProduceAsync("user-profiles", userId, $"{{\"name\": \"{userId}\", \"status\": \"active\"}}");
}

// Second round - update status
foreach (var userId in users)
{
    await producer.ProduceAsync("user-profiles", userId, $"{{\"name\": \"{userId}\", \"status\": \"premium\"}}");
}

// Third round - another update
foreach (var userId in users)
{
    await producer.ProduceAsync("user-profiles", userId, $"{{\"name\": \"{userId}\", \"status\": \"vip\"}}");
}

await producer.FlushAsync();
Console.WriteLine("Produced 9 messages (3 updates per user)");
```

## Step 5: Trigger Compaction

Use the native client to trigger log compaction:

```csharp
Console.WriteLine("Triggering compaction...");
var compactionResult = await nativeClient.Cluster.TriggerLogCompactionAsync();
Console.WriteLine($"Compaction completed: {compactionResult.RecordsRemoved} records removed");
await Task.Delay(500); // Allow compaction to complete
```

## Step 6: Read Compacted Log

After compaction, only the latest value per key should remain:

```csharp
consumer.Subscribe("user-profiles");

var latestValues = new Dictionary<string, string>();

while (true)
{
    var message = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (message == null) break;

    latestValues[message.Key!] = message.Value ?? "(tombstone)";
    Console.WriteLine($"Key: {message.Key}, Value: {message.Value}");
}

Console.WriteLine($"Total unique keys: {latestValues.Count}");
// Should be 3 (one per user)
```

## Step 7: Use Tombstones for Deletion

Delete a user by sending a null value:

```csharp
// Delete user-2 using tombstone
await producer.ProduceAsync("user-profiles", "user-2", null!);
await producer.FlushAsync();
Console.WriteLine("Sent tombstone for user-2");

// Trigger compaction again
await nativeClient.Cluster.TriggerLogCompactionAsync();

// Wait for tombstone retention to expire
await Task.Delay(1500);
```

## Step 8: Verify Deletion

Read the topic again to verify the key was removed:

```csharp
await using var verifyConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

verifyConsumer.Subscribe("user-profiles");

var remainingUsers = new Dictionary<string, string>();
while (true)
{
    var msg = await verifyConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (msg == null) break;

    if (msg.Value != null) // Skip tombstones
        remainingUsers[msg.Key!] = msg.Value;
    else
        remainingUsers.Remove(msg.Key!);
}

Console.WriteLine($"Remaining users: {string.Join(", ", remainingUsers.Keys)}");
// Should only show user-1 and user-3
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. What is the difference between log compaction and time-based retention?
2. What is a tombstone and when would you use one?
3. Why is compaction useful for changelog topics?
4. What happens if you don't set a key when producing to a compacted topic?

---

**Next Lesson:** -> [Lesson 3.7: Ephemeral Topics](../lesson-7/README.md)
