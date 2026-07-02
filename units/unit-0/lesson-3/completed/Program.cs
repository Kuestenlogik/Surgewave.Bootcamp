// Surgewave Bootcamp - Unit 1, Lesson 1: Hello Surgewave
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.1 - Hello Surgewave ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)  // Auto-assign an available port
    .WithStorageEngine(StorageEngines.Memory)  // Use in-memory storage
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Create a Surgewave client connection
Console.WriteLine("Connecting to Surgewave...");

await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

Console.WriteLine("Connected successfully!");
Console.WriteLine();

// Step 3 - List available topics
Console.WriteLine("Available topics:");

var topics = await client.Topics.ListAsync();

if (topics.Count == 0)
{
    Console.WriteLine("  (no topics yet - we'll create some in the next lesson)");
}
else
{
    foreach (var topic in topics)
    {
        Console.WriteLine($"  - {topic.Name} ({topic.PartitionCount} partitions)");
    }
}
Console.WriteLine();

// Step 4 - Clean shutdown (handled by await using)
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
