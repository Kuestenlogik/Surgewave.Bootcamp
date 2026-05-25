# Lesson 18.4: Instructions

Follow these step-by-step instructions to implement custom checkpointing.

## Step 1: Define Checkpoint Interface

Create an interface for checkpoint storage:

```csharp
interface ICheckpointStore
{
    Task<Dictionary<(string Topic, int Partition), long>> LoadAsync();
    Task SaveAsync(string topic, int partition, long offset);
    Task SaveAllAsync(Dictionary<(string Topic, int Partition), long> offsets);
}
```

## Step 2: Implement File-Based Checkpoint Store

Create a simple file-based implementation:

```csharp
class FileCheckpointStore : ICheckpointStore
{
    private readonly string _filePath;

    public FileCheckpointStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<Dictionary<(string Topic, int Partition), long>> LoadAsync()
    {
        var offsets = new Dictionary<(string, int), long>();

        if (!File.Exists(_filePath))
            return offsets;

        var lines = await File.ReadAllLinesAsync(_filePath);
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length == 3)
            {
                var topic = parts[0];
                var partition = int.Parse(parts[1]);
                var offset = long.Parse(parts[2]);
                offsets[(topic, partition)] = offset;
            }
        }

        return offsets;
    }

    public async Task SaveAsync(string topic, int partition, long offset)
    {
        var offsets = await LoadAsync();
        offsets[(topic, partition)] = offset;
        await SaveAllAsync(offsets);
    }

    public async Task SaveAllAsync(Dictionary<(string Topic, int Partition), long> offsets)
    {
        var lines = offsets.Select(kvp =>
            $"{kvp.Key.Topic}|{kvp.Key.Partition}|{kvp.Value}");
        await File.WriteAllLinesAsync(_filePath, lines);
    }
}
```

## Step 3: Load Checkpoint on Startup

Initialize consumer position from checkpoint:

```csharp
var checkpointStore = new FileCheckpointStore("checkpoints.txt");

// Load saved checkpoints
var checkpoints = await checkpointStore.LoadAsync();
Console.WriteLine($"Loaded {checkpoints.Count} checkpoints");

// Apply checkpoints to consumer
foreach (var ((topic, partition), offset) in checkpoints)
{
    Console.WriteLine($"Restoring {topic}-{partition} to offset {offset}");
    consumer.Seek(topic, partition, offset);
}
```

## Step 4: Save Checkpoint After Processing

Update checkpoint after each message (or batch):

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    // Process the message
    await ProcessMessageAsync(result);

    // Save checkpoint (the NEXT offset to consume)
    await checkpointStore.SaveAsync(
        result.Topic,
        result.Partition,
        result.Offset + 1);

    Console.WriteLine($"Checkpointed offset {result.Offset + 1}");
}
```

## Step 5: Batch Checkpointing for Performance

For better performance, checkpoint periodically:

```csharp
var pendingOffsets = new Dictionary<(string, int), long>();
var lastCheckpointTime = DateTime.UtcNow;
var checkpointInterval = TimeSpan.FromSeconds(5);

while (!cancellationToken.IsCancellationRequested)
{
    var result = await consumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    await ProcessMessageAsync(result);

    // Track highest offset per partition
    pendingOffsets[(result.Topic, result.Partition)] = result.Offset + 1;

    // Checkpoint periodically
    if (DateTime.UtcNow - lastCheckpointTime > checkpointInterval)
    {
        await checkpointStore.SaveAllAsync(pendingOffsets);
        Console.WriteLine($"Checkpointed {pendingOffsets.Count} partitions");
        lastCheckpointTime = DateTime.UtcNow;
    }
}

// Final checkpoint on shutdown
await checkpointStore.SaveAllAsync(pendingOffsets);
```

## Step 6: Exactly-Once with Database Transaction

Coordinate checkpoint with database write:

```csharp
// Simulated database context
class ProcessingState
{
    public List<string> ProcessedOrders { get; } = new();
    public Dictionary<(string, int), long> Checkpoints { get; } = new();

    public void ProcessInTransaction(ConsumeResult<string, string> result)
    {
        // In a real app, this would be a DB transaction
        // Both operations succeed or fail together

        // 1. "Write" to database
        ProcessedOrders.Add(result.Value!);

        // 2. Update checkpoint
        Checkpoints[(result.Topic, result.Partition)] = result.Offset + 1;
    }
}
```

## Step 7: Handle Recovery

Test recovery by simulating a crash:

```csharp
// First run - process some messages, then "crash"
for (int i = 0; i < 5; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    Console.WriteLine($"Processing: {result.Value}");
    await checkpointStore.SaveAsync(result.Topic, result.Partition, result.Offset + 1);

    if (i == 2)
    {
        Console.WriteLine("Simulating crash!");
        break; // Crash after 3 messages
    }
}

// Simulate restart - reload checkpoint
Console.WriteLine("\nRestarting after crash...");
var savedCheckpoints = await checkpointStore.LoadAsync();

// Seek to saved position
foreach (var ((topic, partition), offset) in savedCheckpoints)
{
    consumer.Seek(topic, partition, offset);
    Console.WriteLine($"Resumed from offset {offset}");
}

// Continue processing from checkpoint
// Will NOT reprocess messages 0, 1, 2
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- File-based checkpoint store
- Checkpoint loading on startup
- Checkpoint saving after processing
- Recovery after simulated crash
- Batch checkpointing for performance

## Sample Output

```
=== Surgewave Bootcamp: Lesson 18.4 - Custom Checkpointing ===

Surgewave broker started on port 54321
Checkpoint file: C:\temp\checkpoints.txt

Producing 10 test messages...

--- Initial Run (will "crash" after 5 messages) ---
No existing checkpoints, starting from beginning
Processing: Order-1 at offset 0
  Checkpointed: orders-0 -> 1
Processing: Order-2 at offset 1
  Checkpointed: orders-0 -> 2
Processing: Order-3 at offset 2
  Checkpointed: orders-0 -> 3
Processing: Order-4 at offset 3
  Checkpointed: orders-0 -> 4
Processing: Order-5 at offset 4
  Checkpointed: orders-0 -> 5

SIMULATING CRASH!

--- Recovery Run ---
Loaded checkpoint: orders-0 -> 5
Seeking to saved offset 5...
Processing: Order-6 at offset 5
  Checkpointed: orders-0 -> 6
Processing: Order-7 at offset 6
  Checkpointed: orders-0 -> 7
...

No duplicate processing! Recovery successful.

Lesson complete!
```

## Knowledge Check

1. Why store checkpoints externally instead of using Surgewave's offset commit?
2. How does storing the checkpoint with data enable exactly-once?
3. What's the trade-off of per-message vs periodic checkpointing?
4. How would you handle checkpoint storage failure?
5. Why save `offset + 1` instead of `offset`?

---

**Next:** [Lesson 18.5: Partition Assignment](../lesson-5/README.md)
