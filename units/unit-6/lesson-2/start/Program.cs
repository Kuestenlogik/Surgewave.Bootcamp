// Surgewave Bootcamp - Unit 18, Lesson 2: Seek Operations
//
// In this lesson, you'll learn:
// - Seek to the beginning of a partition
// - Seek to a specific offset
// - Seek to the end (latest offset)
// - Use position to track current offset
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 18.2 - Seek Operations ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

// Variables used in TODO sections below
#pragma warning disable CS0219
var topic = "seek-demo-topic";
var partition = 0;
#pragma warning restore CS0219

// TODO: Step 1 - Create a producer and produce test messages
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// Console.WriteLine("\nProducing 10 test messages...");
// for (int i = 1; i <= 10; i++)
// {
//     var offset = await producer.ProduceAsync(topic, $"key-{i}", $"Message-{i}");
//     Console.WriteLine($"Produced Message-{i} at offset {offset}");
//     await Task.Delay(50); // Small delay for distinct timestamps
// }

// TODO: Step 2 - Create a consumer (standalone, no consumer group)
// await using var consumer = new SurgewaveConsumer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// // Assign partition directly (no consumer group coordination)
// consumer.Assign(topic, partition, offset: 0);

// TODO: Step 3 - Seek to beginning and consume
// Console.WriteLine("\n--- Seek to Beginning ---");
// consumer.Seek(topic, partition, offset: 0);
// Console.WriteLine("Seeking to offset 0...");
//
// var result = await consumer.ConsumeAsync();
// if (result != null)
// {
//     Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
// }

// TODO: Step 4 - Seek to specific offset (offset 5)
// Console.WriteLine("\n--- Seek to Specific Offset ---");
// consumer.Seek(topic, partition, offset: 5);
// Console.WriteLine("Seeking to offset 5...");
//
// result = await consumer.ConsumeAsync();
// if (result != null)
// {
//     Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
// }
//
// result = await consumer.ConsumeAsync();
// if (result != null)
// {
//     Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
// }

// TODO: Step 5 - Check current position
// Console.WriteLine("\n--- Current Position ---");
// var currentPosition = consumer.Position(topic, partition);
// Console.WriteLine($"Current position: offset {currentPosition}");

// TODO: Step 6 - Seek to end and wait for new message
// Console.WriteLine("\n--- Seek to End ---");
// // Seek to offset 10 (past all existing messages)
// consumer.Seek(topic, partition, offset: 10);
// Console.WriteLine("Seeking to latest offset (10)...");
//
// // Produce a new message
// Console.WriteLine("Producing new message...");
// await producer.ProduceAsync(topic, "new-key", "NewMessage");
//
// result = await consumer.ConsumeAsync();
// if (result != null)
// {
//     Console.WriteLine($"Consumed: {result.Value} at offset {result.Offset}");
// }

// TODO: Step 7 - Demonstrate skip pattern for bad messages
// Console.WriteLine("\n--- Skip Bad Message Pattern ---");
// consumer.Seek(topic, partition, offset: 0);
//
// for (int i = 0; i < 5; i++)
// {
//     var position = consumer.Position(topic, partition);
//     try
//     {
//         result = await consumer.ConsumeAsync();
//         if (result == null) continue;
//
//         // Simulate error on offset 2
//         if (result.Offset == 2)
//         {
//             throw new Exception("Simulated error - bad message");
//         }
//
//         Console.WriteLine($"Processing offset {result.Offset}: {result.Value}");
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Error at offset {position}: {ex.Message}");
//         // Skip the bad message by seeking past it
//         consumer.Seek(topic, partition, position + 1);
//         Console.WriteLine($"Skipping to offset {position + 1}...");
//     }
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Seek(topic, partition, offset) jumps to any position");
Console.WriteLine("2. Position() returns the next offset to be consumed");
Console.WriteLine("3. Seek to 0 replays from beginning");
Console.WriteLine("4. Seek past end waits for new messages");
Console.WriteLine("5. Use seek to skip corrupted/bad messages");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
