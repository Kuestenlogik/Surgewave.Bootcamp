// Surgewave Bootcamp - Unit 1, Lesson 5: Partitioning
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.5 - Partitioning ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(false)  // We'll create the topic manually
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Create a topic with multiple partitions
Console.WriteLine("Creating topic 'user-events' with 3 partitions...");

await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

await client.Topics.CreateAsync("user-events", partitions: 3);

Console.WriteLine("Topic created!");
Console.WriteLine();

// Step 3 - Produce messages with keys
Console.WriteLine("Producing user events...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var events = new[]
{
    ("user-1", "login"),
    ("user-2", "login"),
    ("user-3", "login"),
    ("user-1", "view-product"),
    ("user-2", "add-to-cart"),
    ("user-1", "add-to-cart"),
    ("user-3", "view-product"),
    ("user-1", "checkout"),
    ("user-2", "checkout"),
    ("user-3", "add-to-cart"),
};

foreach (var (userId, eventType) in events)
{
    var result = await producer.ProduceAsync("user-events", userId, eventType);
    Console.WriteLine($"  {userId}: {eventType} -> Partition {result.Partition}");
}

Console.WriteLine();

// Step 4 - List topic details
Console.WriteLine("Listing topic partitions...");

var topics = await client.Topics.ListAsync();
foreach (var topic in topics)
{
    Console.WriteLine($"  Topic: {topic.Name}");
    Console.WriteLine($"  Partitions: {topic.PartitionCount}");
}

Console.WriteLine();

// Step 5 - Consume from all partitions
Console.WriteLine("Consuming from all partitions...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("user-events");

var partitionCounts = new Dictionary<int, int>();
int totalConsumed = 0;
const int expectedMessages = 10;

while (totalConsumed < expectedMessages)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  [P{result.Partition}] {result.Key}: {result.Value}");

        if (!partitionCounts.ContainsKey(result.Partition))
            partitionCounts[result.Partition] = 0;
        partitionCounts[result.Partition]++;

        totalConsumed++;
    }
}

Console.WriteLine();
Console.WriteLine("Message distribution by partition:");
foreach (var (partition, count) in partitionCounts.OrderBy(p => p.Key))
{
    Console.WriteLine($"  Partition {partition}: {count} messages");
}

Console.WriteLine();

// Step 6 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
