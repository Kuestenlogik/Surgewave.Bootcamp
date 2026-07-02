// Surgewave Bootcamp - Unit 18, Lesson 1: Manual Offset Management
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Abstractions;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 18.1 - Manual Offset Management ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders-topic";
var groupId = "manual-commit-group";

// Step 1 - Create a producer to send test messages
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

// Step 2 - Produce test messages
Console.WriteLine("\nProducing test messages...");
for (int i = 1; i <= 10; i++)
{
    var key = $"key-{i}";
    var value = $"Order-{i}";
    await producer.ProduceAsync(topic, key, value);
}
Console.WriteLine($"Produced 10 messages to {topic}");

// Step 3 - Create a consumer with auto-commit DISABLED
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = groupId;
    options.EnableAutoCommit = false;  // Key setting for manual commit!
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

Console.WriteLine();
Console.WriteLine("Configuration:");
Console.WriteLine("  Auto-commit: Disabled");
Console.WriteLine($"  Group ID: {groupId}");

// Step 4 - Subscribe to the topic
await consumer.SubscribeAsync(default, topic);

// Step 5 - Implement at-least-once delivery (commit AFTER processing)
Console.WriteLine("\n--- At-Least-Once Demo ---");
Console.WriteLine("(Processing 5 messages with per-message commit)");
Console.WriteLine();

for (int i = 0; i < 5; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    Console.WriteLine($"Received: {result.Value} at offset {result.Offset}");

    // Simulate processing
    Console.WriteLine($"  Processing {result.Value}...");
    await Task.Delay(100); // Simulate work

    // Commit AFTER processing (at-least-once semantics)
    // If we crash here, the message will be redelivered
    await consumer.CommitAsync(result);
    Console.WriteLine($"  Committed offset {result.Offset + 1}");
}

// Step 6 - Demonstrate batch commit for better performance
Console.WriteLine("\n--- Batch Commit Demo ---");
Console.WriteLine("(Processing remaining messages with batch commit, batch size = 3)");
Console.WriteLine();

var batch = new List<ConsumeResult<string, string>>();
var batchSize = 3;
var messagesProcessed = 0;

while (messagesProcessed < 5)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    Console.WriteLine($"Received: {result.Value} at offset {result.Offset}");
    batch.Add(result);
    messagesProcessed++;

    // Process the message
    await Task.Delay(50); // Simulate work

    // Commit when batch is full
    if (batch.Count >= batchSize)
    {
        // Get the highest offset from each partition
        var offsets = batch
            .GroupBy(r => (r.Topic, r.Partition))
            .Select(g => new TopicPartitionOffset(
                g.Key.Topic,
                g.Key.Partition,
                g.Max(r => r.Offset) + 1))
            .ToList();

        await consumer.CommitAsync(offsets);
        Console.WriteLine($"  --> Batch commit: {batch.Count} messages (up to offset {offsets.First().Offset})");
        batch.Clear();
    }
}

// Commit any remaining messages (important for graceful shutdown!)
if (batch.Count > 0)
{
    var offsets = batch
        .GroupBy(r => (r.Topic, r.Partition))
        .Select(g => new TopicPartitionOffset(
            g.Key.Topic,
            g.Key.Partition,
            g.Max(r => r.Offset) + 1))
        .ToList();
    await consumer.CommitAsync(offsets);
    Console.WriteLine($"  --> Final batch commit: {batch.Count} messages (up to offset {offsets.First().Offset})");
}

// Demonstrate at-most-once pattern (for comparison)
Console.WriteLine("\n--- At-Most-Once Pattern (Explanation) ---");
Console.WriteLine("For at-most-once, you would commit BEFORE processing:");
Console.WriteLine("  1. Receive message");
Console.WriteLine("  2. Commit offset immediately");
Console.WriteLine("  3. Process message");
Console.WriteLine("  Risk: If crash during step 3, message is lost forever");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Set EnableAutoCommit = false for manual control");
Console.WriteLine("2. At-least-once: Commit AFTER processing (may reprocess on crash)");
Console.WriteLine("3. At-most-once: Commit BEFORE processing (may lose on crash)");
Console.WriteLine("4. Batch commits improve throughput (fewer round-trips)");
Console.WriteLine("5. Always commit remaining offsets on shutdown");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
