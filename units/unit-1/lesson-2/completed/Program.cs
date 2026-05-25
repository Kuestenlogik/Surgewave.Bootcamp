// Surgewave Bootcamp - Unit 1, Lesson 3: Consuming Messages
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.3 - Consuming Messages ===");
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

// Step 2 - First, produce some messages
Console.WriteLine("Producing messages first...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var messages = new[]
{
    ("user-1", "Hello Surgewave!"),
    ("user-2", "Surgewave is fast!"),
    ("user-1", "And easy to use!")
};

foreach (var (key, value) in messages)
{
    await producer.ProduceAsync("greetings", key, value);
    Console.WriteLine($"  Produced: {value}");
}

Console.WriteLine();

// Step 3 - Create a consumer
Console.WriteLine("Creating consumer...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("greetings");

Console.WriteLine("Consumer ready!");
Console.WriteLine();

// Step 4 - Consume messages
Console.WriteLine("Consuming messages...");

int messageCount = 0;
const int expectedMessages = 3;

while (messageCount < expectedMessages)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  Received [{result.Offset}]: Key='{result.Key}', Value='{result.Value}'");
        messageCount++;
    }
}

Console.WriteLine();
Console.WriteLine($"Consumed {messageCount} messages from 'greetings' topic");
Console.WriteLine();

// Step 5 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
