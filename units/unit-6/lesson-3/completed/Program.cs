// Surgewave Bootcamp - Unit 18, Lesson 3: Pause & Resume
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 18.3 - Pause & Resume ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "pause-demo-topic";
var partition = 0;

// Step 1 - Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Assign(topic, partition, offset: 0);

// Step 2 - Basic pause and resume demo
Console.WriteLine("\n--- Basic Pause/Resume Demo ---");

// Produce some messages
Console.WriteLine("Producing messages...");
await producer.ProduceAsync(topic, "key-1", "Message-1");
await producer.ProduceAsync(topic, "key-2", "Message-2");
await producer.ProduceAsync(topic, "key-3", "Message-3");

// Consume first message
var result = await consumer.ConsumeAsync();
Console.WriteLine($"Before pause: {result?.Value}");

// Pause the partition
Console.WriteLine("Pausing partition 0...");
consumer.Pause((topic, partition));

// Show paused partitions
Console.WriteLine($"Paused partitions: [{string.Join(", ", consumer.PausedPartitions)}]");

// Try to consume - should return null because partition is paused
result = await consumer.ConsumeAsync();
Console.WriteLine($"While paused: {result?.Value ?? "null (no messages from paused partition)"}");

// Resume the partition
Console.WriteLine("Resuming partition 0...");
consumer.Resume((topic, partition));

// Consume should work again
result = await consumer.ConsumeAsync();
Console.WriteLine($"After resume: {result?.Value}");

// Step 3 - Backpressure handling demo
Console.WriteLine("\n--- Backpressure Demo ---");
Console.WriteLine("Simulating a downstream system with limited capacity...");
Console.WriteLine();

var queueDepth = 0;
var maxQueueDepth = 3;
var processedCount = 0;

// Produce more messages
for (int i = 4; i <= 12; i++)
{
    await producer.ProduceAsync(topic, $"key-{i}", $"Message-{i}");
}

while (processedCount < 7)
{
    // Check if we need to pause due to backpressure
    if (queueDepth >= maxQueueDepth && !consumer.PausedPartitions.Contains((topic, partition)))
    {
        Console.WriteLine($"  [BACKPRESSURE] Queue depth {queueDepth} >= {maxQueueDepth}, PAUSING...");
        consumer.Pause((topic, partition));
    }

    // Check if we can resume (queue drained to half)
    if (queueDepth <= maxQueueDepth / 2 && consumer.PausedPartitions.Contains((topic, partition)))
    {
        Console.WriteLine($"  [RECOVERED] Queue depth {queueDepth} <= {maxQueueDepth / 2}, RESUMING...");
        consumer.Resume((topic, partition));
    }

    // Try to consume (will return null if paused)
    result = await consumer.ConsumeAsync();
    if (result != null)
    {
        queueDepth++;
        Console.WriteLine($"Received: {result.Value}, queue depth: {queueDepth}");
    }

    // Simulate processing (drains queue)
    if (queueDepth > 0)
    {
        await Task.Delay(100); // Simulate work
        queueDepth--;
        processedCount++;
        Console.WriteLine($"  Processed message, queue depth now: {queueDepth}");
    }
    else if (consumer.PausedPartitions.Contains((topic, partition)))
    {
        // Wait a bit while paused
        await Task.Delay(50);
    }
}

// Step 4 - Rate limiting demo
Console.WriteLine("\n--- Rate Limiting Demo ---");
Console.WriteLine("Limiting to 3 messages per second...");
Console.WriteLine();

var messagesThisSecond = 0;
var maxPerSecond = 3;
var windowStart = DateTime.UtcNow;
var rateProcessed = 0;

// Reset consumer position
consumer.Seek(topic, partition, 0);

while (rateProcessed < 8)
{
    var now = DateTime.UtcNow;

    // Reset window every second
    if ((now - windowStart).TotalSeconds >= 1)
    {
        if (messagesThisSecond > 0)
        {
            Console.WriteLine($"  --- Window complete: {messagesThisSecond} messages ---");
        }

        windowStart = now;
        messagesThisSecond = 0;

        if (consumer.PausedPartitions.Contains((topic, partition)))
        {
            Console.WriteLine("  [WINDOW RESET] RESUMING...");
            consumer.Resume((topic, partition));
        }
    }

    // Pause if rate limit reached
    if (messagesThisSecond >= maxPerSecond && !consumer.PausedPartitions.Contains((topic, partition)))
    {
        Console.WriteLine($"  [RATE LIMIT] {messagesThisSecond} messages this second, PAUSING...");
        consumer.Pause((topic, partition));
    }

    // Try to consume
    result = await consumer.ConsumeAsync();
    if (result != null)
    {
        messagesThisSecond++;
        rateProcessed++;
        Console.WriteLine($"Processed: {result.Value} ({messagesThisSecond}/{maxPerSecond} this second)");
    }

    await Task.Delay(50);
}

// Step 5 - Show final state
Console.WriteLine("\n--- Final State ---");
Console.WriteLine($"Paused partitions: [{string.Join(", ", consumer.PausedPartitions)}]");
if (consumer.PausedPartitions.Count == 0)
{
    Console.WriteLine("All partitions are active.");
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Pause((topic, partition)) stops consumption from that partition");
Console.WriteLine("2. Resume((topic, partition)) restarts consumption");
Console.WriteLine("3. PausedPartitions shows currently paused partitions");
Console.WriteLine("4. Use for backpressure, priority, and rate limiting");
Console.WriteLine("5. Messages stay in broker - nothing is lost while paused");
Console.WriteLine("6. ConsumeAsync() returns null when all assigned partitions are paused");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
