# Lesson 18.1: Instructions

Follow these step-by-step instructions to implement manual offset management.

## Step 1: Understand the Configuration

To disable auto-commit, set `EnableAutoCommit` to `false`:

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "manual-commit-group";
    options.EnableAutoCommit = false;  // Key setting!
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});
```

With auto-commit disabled, offsets are only committed when you explicitly call `CommitAsync()`.

## Step 2: Implement At-Least-Once Delivery

For at-least-once delivery, commit the offset AFTER successfully processing each message:

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    // 1. Process the message first
    await ProcessMessageAsync(result);

    // 2. Commit only after successful processing
    await consumer.CommitAsync(result, cancellationToken);
}
```

If the application crashes after processing but before committing, the message will be redelivered on restart.

## Step 3: Implement At-Most-Once Delivery

For at-most-once delivery, commit the offset BEFORE processing:

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    // 1. Commit immediately (before processing)
    await consumer.CommitAsync(result, cancellationToken);

    // 2. Then process the message
    await ProcessMessageAsync(result);
}
```

If the application crashes during processing, the message is lost but never reprocessed.

## Step 4: Implement Batch Commit

For better performance, accumulate messages and commit in batches:

```csharp
var batchSize = 10;
var uncommittedMessages = new List<ConsumeResult<string, string>>();

while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    await ProcessMessageAsync(result);
    uncommittedMessages.Add(result);

    // Commit when batch is full
    if (uncommittedMessages.Count >= batchSize)
    {
        // Commit the highest offset for each partition
        var offsets = uncommittedMessages
            .GroupBy(r => (r.Topic, r.Partition))
            .Select(g => new TopicPartitionOffset(
                g.Key.Topic,
                g.Key.Partition,
                g.Max(r => r.Offset) + 1))
            .ToList();

        await consumer.CommitAsync(offsets, cancellationToken);
        uncommittedMessages.Clear();
    }
}
```

## Step 5: Handle Commit Errors

Commit operations can fail. Handle errors appropriately:

```csharp
try
{
    await consumer.CommitAsync(result, cancellationToken);
    Console.WriteLine($"Committed offset {result.Offset + 1}");
}
catch (Exception ex)
{
    Console.WriteLine($"Commit failed: {ex.Message}");
    // Decide: retry, skip, or fail
}
```

## Step 6: Commit on Shutdown

Always commit pending offsets before shutting down:

```csharp
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        // ... consume and process ...
    }
}
finally
{
    // Commit any remaining offsets before exit
    await consumer.CommitAsync();
    Console.WriteLine("Final commit completed");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Consumer with auto-commit disabled
- At-least-once delivery pattern
- Batch commit for performance
- Graceful shutdown with final commit

## Sample Output

```
=== Surgewave Bootcamp: Lesson 18.1 - Manual Offset Management ===

Configuration:
  Auto-commit: Disabled
  Group ID: manual-commit-group

Producing test messages...
Produced 10 messages to orders-topic

--- At-Least-Once Demo ---
Received: Order-1 at offset 0
Processing Order-1...
Committed offset 1
Received: Order-2 at offset 1
Processing Order-2...
Committed offset 2
...

--- Batch Commit Demo ---
Received: Order-1 at offset 0
Received: Order-2 at offset 1
Received: Order-3 at offset 2
Received: Order-4 at offset 3
Received: Order-5 at offset 4
Batch commit: 5 messages (offsets up to 5)
...

Final commit completed.

Lesson complete!
```

## Knowledge Check

1. What happens if you crash between processing and committing with at-least-once semantics?
2. Why is batch commit more performant than per-message commit?
3. When would you choose at-most-once over at-least-once?
4. What's the risk of using a very large batch size for commits?
5. Why is it important to commit on graceful shutdown?

---

**Next:** [Lesson 18.2: Seek Operations](../lesson-2/README.md)
