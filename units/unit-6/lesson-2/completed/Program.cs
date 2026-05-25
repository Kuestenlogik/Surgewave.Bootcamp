// Surgewave Bootcamp - Unit 18, Lesson 2: Seek Operations
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 18.2 - Seek Operations ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "seek-demo-topic";
var partition = 0;

// Step 1 - Create a producer and produce test messages
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\nProducing 10 test messages...");
for (int i = 1; i <= 10; i++)
{
    var offset = await producer.ProduceAsync(topic, $"key-{i}", $"Message-{i}");
    Console.WriteLine($"Produced Message-{i} at offset {offset}");
    await Task.Delay(50); // Small delay for distinct timestamps
}

// Step 2 - Create a consumer (standalone, no consumer group)
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

// Assign partition directly (no consumer group coordination)
consumer.Assign(topic, partition, offset: 0);

// Step 3 - Seek to beginning and consume
Console.WriteLine("\n--- Seek to Beginning ---");
consumer.Seek(topic, partition, offset: 0);
Console.WriteLine("Seeking to offset 0...");

var result = await consumer.ConsumeAsync();
if (result != null)
{
    Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
}

// Step 4 - Seek to specific offset (offset 5)
Console.WriteLine("\n--- Seek to Specific Offset ---");
consumer.Seek(topic, partition, offset: 5);
Console.WriteLine("Seeking to offset 5...");

result = await consumer.ConsumeAsync();
if (result != null)
{
    Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
}

result = await consumer.ConsumeAsync();
if (result != null)
{
    Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
}

// Step 5 - Check current position
Console.WriteLine("\n--- Current Position ---");
var currentPosition = consumer.Position(topic, partition);
Console.WriteLine($"Current position: offset {currentPosition}");

// Step 6 - Seek to end and wait for new message
Console.WriteLine("\n--- Seek to End ---");
// Seek to offset 10 (past all existing messages)
consumer.Seek(topic, partition, offset: 10);
Console.WriteLine("Seeking to latest offset (10)...");

// Produce a new message
Console.WriteLine("Producing new message...");
await producer.ProduceAsync(topic, "new-key", "NewMessage");

result = await consumer.ConsumeAsync();
if (result != null)
{
    Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
}

// Step 7 - Demonstrate skip pattern for bad messages
Console.WriteLine("\n--- Skip Bad Message Pattern ---");
consumer.Seek(topic, partition, offset: 0);
Console.WriteLine("Seeking back to offset 0 for skip demo...");
Console.WriteLine();

for (int i = 0; i < 6; i++)
{
    var position = consumer.Position(topic, partition);
    try
    {
        result = await consumer.ConsumeAsync();
        if (result == null) continue;

        // Simulate error on offset 2
        if (result.Offset == 2)
        {
            throw new InvalidOperationException("Simulated error - bad message format");
        }

        Console.WriteLine($"Processing offset {result.Offset}: {result.Value}");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"Error at offset {position}: {ex.Message}");
        // Skip the bad message by seeking past it
        consumer.Seek(topic, partition, position + 1);
        Console.WriteLine($"Skipping to offset {position + 1}...");
    }
}

// Demonstrate seeking backwards
Console.WriteLine("\n--- Seeking Backwards ---");
Console.WriteLine("Seeking back to offset 3 to reprocess...");
consumer.Seek(topic, partition, offset: 3);

result = await consumer.ConsumeAsync();
if (result != null)
{
    Console.WriteLine($"Reprocessed: {result.Value} at offset {result.Offset}");
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Seek(topic, partition, offset) jumps to any position");
Console.WriteLine("2. Position() returns the next offset to be consumed");
Console.WriteLine("3. Seek to 0 replays from beginning");
Console.WriteLine("4. Seek past end waits for new messages");
Console.WriteLine("5. Use seek to skip corrupted/bad messages");
Console.WriteLine("6. Seek can go backwards to reprocess messages");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
