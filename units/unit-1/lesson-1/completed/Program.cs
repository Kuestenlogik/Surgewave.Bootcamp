// Surgewave Bootcamp - Unit 1, Lesson 2: Producing Messages
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.2 - Producing Messages ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Create a producer
Console.WriteLine("Creating producer...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("Producer ready!");
Console.WriteLine();

// Step 3 - Produce messages
Console.WriteLine("Producing messages...");

var messages = new[]
{
    ("user-1", "Hello Surgewave!"),
    ("user-2", "Surgewave is fast!"),
    ("user-1", "And easy to use!")
};

foreach (var (key, value) in messages)
{
    var result = await producer.ProduceAsync("greetings", key, value);
    Console.WriteLine($"  Message: key='{key}', value='{value}' -> Offset {result.Offset}");
}

Console.WriteLine();
Console.WriteLine($"Produced {messages.Length} messages to 'greetings' topic");
Console.WriteLine();

// Step 4 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
