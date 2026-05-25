// Surgewave Bootcamp - Unit 18, Lesson 5: Partition Assignment
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 18.5 - Partition Assignment ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders";

// Step 1 - Create producer and produce test messages
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\nProducing test messages...");
for (int i = 1; i <= 10; i++)
{
    await producer.ProduceAsync(topic, $"key-{i}", $"Order-{i}");
}
Console.WriteLine("Messages produced.");

// Step 2 - Manual assignment demo (no consumer group)
Console.WriteLine("\n--- Manual Assignment Demo ---");
Console.WriteLine("Creating standalone consumer (no GroupId)...");

await using var standaloneConsumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    // No GroupId = manual assignment mode
});

// Manually assign specific partitions
Console.WriteLine("Manually assigning partitions...");
standaloneConsumer.Assign(topic, partition: 0, offset: 0);
Console.WriteLine($"Assigned: {topic}-0 at offset 0");

// Check assignment
var assignment = standaloneConsumer.Assignment;
Console.WriteLine($"Current assignment: [{string.Join(", ", assignment.Select(a => $"{a.topic}-{a.partition}"))}]");

// Consume a few messages
Console.WriteLine("\nConsuming from manually assigned partition:");
for (int i = 0; i < 3; i++)
{
    var result = await standaloneConsumer.ConsumeAsync();
    if (result != null)
    {
        Console.WriteLine($"  Received: {result.Value} from {result.Topic}-{result.Partition}");
    }
}

// Step 3 - Demonstrate assignment strategy comparison
Console.WriteLine("\n--- Assignment Strategy Comparison ---");
Console.WriteLine("Topic: orders (6 partitions)");
Console.WriteLine("Consumers: C1, C2, C3");

var consumers = new List<string> { "C1", "C2", "C3" };
var partitionCount = 6;

// Range assignment
Console.WriteLine("\nRange Assignment (consecutive partitions):");
ShowRangeAssignment(consumers, partitionCount);

// RoundRobin assignment
Console.WriteLine("\nRoundRobin Assignment (alternating distribution):");
ShowRoundRobinAssignment(consumers, partitionCount);

// Step 5 - Show rebalance scenario
Console.WriteLine("\n--- Rebalance Scenario ---");
Console.WriteLine("Shows how assignments change when consumers join/leave");

Console.WriteLine("\nInitial state (2 consumers, Range assignment):");
ShowRangeAssignment(["C1", "C2"], 6);

Console.WriteLine("\nAfter C3 joins (3 consumers):");
ShowRangeAssignment(["C1", "C2", "C3"], 6);

Console.WriteLine("\nAfter C2 leaves (2 consumers remaining):");
ShowRangeAssignment(["C1", "C3"], 6);

// Step 6 - Show uneven partition distribution
Console.WriteLine("\n--- Uneven Distribution Example ---");
Console.WriteLine("7 partitions, 3 consumers (Range assignment):");
ShowRangeAssignment(["C1", "C2", "C3"], 7);
Console.WriteLine("Note: C1 gets 3 partitions, others get 2 (7/3 = 2 remainder 1)");

// Step 7 - Worker-based partition assignment
Console.WriteLine("\n--- Worker-Based Assignment Demo ---");
Console.WriteLine("For external orchestration (Kubernetes, etc.):");
Console.WriteLine();

// Simulate two workers, each with specific partitions
var worker0Partitions = new[] { 0, 1, 2 };
var worker1Partitions = new[] { 3, 4, 5 };

Console.WriteLine("Worker 0 (WORKER_ID=0) handles: P" + string.Join(", P", worker0Partitions));
Console.WriteLine("Worker 1 (WORKER_ID=1) handles: P" + string.Join(", P", worker1Partitions));
Console.WriteLine();
Console.WriteLine("Code pattern:");
Console.WriteLine("  var workerId = Environment.GetEnvironmentVariable(\"WORKER_ID\");");
Console.WriteLine("  var partitions = workerPartitionMap[workerId];");
Console.WriteLine("  foreach (var p in partitions)");
Console.WriteLine("      consumer.Assign(topic, p, offset: 0);");

// Step 8 - Sticky assignment explanation
Console.WriteLine("\n--- Sticky Assignment Benefit ---");
Console.WriteLine("Scenario: C3 joins a 2-consumer group");
Console.WriteLine();
Console.WriteLine("Without sticky (full rebalance):");
Console.WriteLine("  C1: P0, P1, P2 -> P0, P2 (gave up P1)");
Console.WriteLine("  C2: P3, P4, P5 -> P1, P4 (gave up P3, P5, got P1)");
Console.WriteLine("  C3: (new)      -> P3, P5");
Console.WriteLine("  Partitions moved: 4");
Console.WriteLine();
Console.WriteLine("With sticky (minimal movement):");
Console.WriteLine("  C1: P0, P1, P2 -> P0, P1 (gave up P2 only)");
Console.WriteLine("  C2: P3, P4, P5 -> P3, P4 (gave up P5 only)");
Console.WriteLine("  C3: (new)      -> P2, P5");
Console.WriteLine("  Partitions moved: 2");
Console.WriteLine();
Console.WriteLine("Benefit: Stateful consumers avoid reloading state unnecessarily");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Manual assignment: Use Assign() without GroupId");
Console.WriteLine("2. Auto assignment: Use Subscribe() with GroupId");
Console.WriteLine("3. Range: Consecutive partitions per consumer");
Console.WriteLine("4. RoundRobin: Even distribution across consumers");
Console.WriteLine("5. Sticky: Minimizes partition movement on rebalance");
Console.WriteLine("6. Handle PartitionsAssigned/Revoked for state management");

Console.WriteLine();
Console.WriteLine("Congratulations! You've completed Unit 18: Advanced Consumer Patterns!");

// Helper methods

void ShowRangeAssignment(List<string> consumerList, int pCount)
{
    var partitionsPerConsumer = pCount / consumerList.Count;
    var extra = pCount % consumerList.Count;
    var partitionIndex = 0;

    for (int i = 0; i < consumerList.Count; i++)
    {
        var count = partitionsPerConsumer + (i < extra ? 1 : 0);
        var partitions = Enumerable.Range(partitionIndex, count).ToList();
        Console.WriteLine($"  {consumerList[i]}: P{string.Join(", P", partitions)}");
        partitionIndex += count;
    }
}

void ShowRoundRobinAssignment(List<string> consumerList, int pCount)
{
    var assignments = consumerList.ToDictionary(c => c, c => new List<int>());

    for (int p = 0; p < pCount; p++)
    {
        var consumer = consumerList[p % consumerList.Count];
        assignments[consumer].Add(p);
    }

    foreach (var (consumer, partitions) in assignments)
    {
        Console.WriteLine($"  {consumer}: P{string.Join(", P", partitions)}");
    }
}
