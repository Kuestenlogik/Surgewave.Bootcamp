# Lesson 18.5: Instructions

Follow these step-by-step instructions to understand and implement partition assignment patterns.

## Step 1: Understand Manual Assignment

For simple cases or when you need full control, use manual assignment:

```csharp
// Create consumer WITHOUT a group ID (standalone mode)
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    // No GroupId - manual assignment mode
});

// Manually assign partitions
consumer.Assign("orders", partition: 0, offset: 0);
consumer.Assign("orders", partition: 1, offset: 0);
consumer.Assign("events", partition: 0, offset: 0);

// Check assignment
var assignment = consumer.Assignment;
Console.WriteLine($"Assigned partitions: {assignment.Count}");
foreach (var (topic, partition) in assignment)
{
    Console.WriteLine($"  - {topic} partition {partition}");
}
```

## Step 2: Use Consumer Groups for Automatic Assignment

With a consumer group, partitions are assigned automatically:

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "my-consumer-group";  // Enable group coordination
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

// Subscribe (assignment happens automatically)
await consumer.SubscribeAsync(default, "orders", "events");

// Partitions are assigned by the group coordinator
var assignment = consumer.Assignment;
Console.WriteLine($"Group assigned {assignment.Count} partitions");
```

## Step 3: Handle Rebalance Events

Listen for partition assignment changes:

```csharp
consumer.PartitionsAssigned += (sender, e) =>
{
    Console.WriteLine("Partitions assigned:");
    foreach (var (topic, partition) in e.Partitions)
    {
        Console.WriteLine($"  + {topic}-{partition}");

        // Initialize state for new partition
        InitializePartitionState(topic, partition);
    }
};

consumer.PartitionsRevoked += (sender, e) =>
{
    Console.WriteLine("Partitions revoked:");
    foreach (var (topic, partition) in e.Partitions)
    {
        Console.WriteLine($"  - {topic}-{partition}");

        // Clean up state before losing partition
        SavePartitionState(topic, partition);
    }
};
```

## Step 4: Implement Stateful Consumer with Rebalance Handling

Maintain per-partition state across rebalances:

```csharp
var partitionStates = new Dictionary<(string, int), PartitionState>();

consumer.PartitionsAssigned += (sender, e) =>
{
    foreach (var tp in e.Partitions)
    {
        // Load state for newly assigned partition
        var state = LoadState(tp) ?? new PartitionState();
        partitionStates[tp] = state;

        // If we have a saved offset, seek to it
        if (state.LastOffset.HasValue)
        {
            consumer.Seek(tp.Topic, tp.Partition, state.LastOffset.Value);
        }
    }
};

consumer.PartitionsRevoked += async (sender, e) =>
{
    // Save state for partitions we're losing
    foreach (var tp in e.Partitions)
    {
        if (partitionStates.TryGetValue(tp, out var state))
        {
            await SaveStateAsync(tp, state);
            partitionStates.Remove(tp);
        }
    }
};
```

## Step 5: Compare Assignment Strategies

Simulate different assignment outcomes:

```csharp
void ShowRangeAssignment(List<string> consumers, int partitionCount)
{
    Console.WriteLine("\n--- Range Assignment ---");
    var partitionsPerConsumer = partitionCount / consumers.Count;
    var extra = partitionCount % consumers.Count;

    var partitionIndex = 0;
    foreach (var consumer in consumers)
    {
        var count = partitionsPerConsumer + (consumers.IndexOf(consumer) < extra ? 1 : 0);
        var partitions = Enumerable.Range(partitionIndex, count).ToList();
        Console.WriteLine($"{consumer}: P{string.Join(", P", partitions)}");
        partitionIndex += count;
    }
}

void ShowRoundRobinAssignment(List<string> consumers, int partitionCount)
{
    Console.WriteLine("\n--- RoundRobin Assignment ---");
    var assignments = consumers.ToDictionary(c => c, c => new List<int>());

    for (int p = 0; p < partitionCount; p++)
    {
        var consumer = consumers[p % consumers.Count];
        assignments[consumer].Add(p);
    }

    foreach (var (consumer, partitions) in assignments)
    {
        Console.WriteLine($"{consumer}: P{string.Join(", P", partitions)}");
    }
}
```

## Step 6: Implement Sticky-Like Custom Assignment

Minimize partition movement on rebalance:

```csharp
class StickyAssignmentState
{
    public Dictionary<string, List<int>> PreviousAssignment { get; set; } = new();
}

List<(string Consumer, int Partition)> AssignSticky(
    List<string> consumers,
    int partitionCount,
    StickyAssignmentState state)
{
    var result = new List<(string, int)>();
    var unassigned = Enumerable.Range(0, partitionCount).ToHashSet();

    // First pass: Keep existing assignments where possible
    foreach (var consumer in consumers)
    {
        if (state.PreviousAssignment.TryGetValue(consumer, out var previous))
        {
            foreach (var partition in previous.Where(p => unassigned.Contains(p)))
            {
                result.Add((consumer, partition));
                unassigned.Remove(partition);
            }
        }
    }

    // Second pass: Distribute remaining partitions
    var consumerIndex = 0;
    foreach (var partition in unassigned)
    {
        result.Add((consumers[consumerIndex % consumers.Count], partition));
        consumerIndex++;
    }

    // Update state for next rebalance
    state.PreviousAssignment = result
        .GroupBy(x => x.Consumer)
        .ToDictionary(g => g.Key, g => g.Select(x => x.Partition).ToList());

    return result;
}
```

## Step 7: Test Manual Assignment for Specific Partitions

Sometimes you need to process specific partitions:

```csharp
// Scenario: Two worker processes, each handling specific partitions
var workerId = Environment.GetEnvironmentVariable("WORKER_ID") ?? "0";
var workerPartitions = workerId switch
{
    "0" => new[] { 0, 1, 2 },
    "1" => new[] { 3, 4, 5 },
    _ => throw new Exception($"Unknown worker: {workerId}")
};

Console.WriteLine($"Worker {workerId} handling partitions: {string.Join(", ", workerPartitions)}");

foreach (var partition in workerPartitions)
{
    consumer.Assign("orders", partition, offset: 0);
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Manual partition assignment
- Consumer group assignment with events
- Simulated Range vs RoundRobin comparison
- Rebalance handling with state management

## Sample Output

```
=== Surgewave Bootcamp: Lesson 18.5 - Partition Assignment ===

Surgewave broker started on port 54321

--- Manual Assignment Demo ---
Manually assigning partitions...
Assigned: orders-0 at offset 0
Assigned: orders-1 at offset 0
Current assignment: [orders-0, orders-1]

--- Assignment Strategy Comparison ---
Topic: orders (6 partitions)
Consumers: C1, C2, C3

Range Assignment:
  C1: P0, P1
  C2: P2, P3
  C3: P4, P5

RoundRobin Assignment:
  C1: P0, P3
  C2: P1, P4
  C3: P2, P5

--- Rebalance Event Demo ---
PartitionsAssigned triggered:
  + orders-0
  + orders-1
Processing from assigned partitions...
...

Lesson complete!
```

## Knowledge Check

1. What's the difference between Subscribe and Assign?
2. When would you use manual assignment over consumer groups?
3. Why is sticky assignment important for stateful consumers?
4. What should you do in PartitionsRevoked?
5. How does Range assignment differ from RoundRobin?

---

**Congratulations!** You've completed Unit 18: Advanced Consumer Patterns!
