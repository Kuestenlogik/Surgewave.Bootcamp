// Surgewave Bootcamp - Unit 1, Lesson 4: Consumer Groups
// COMPLETED SOLUTION

using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 1.4 - Consumer Groups ===");
Console.WriteLine();

// Step 1 - Start an embedded Surgewave broker
Console.WriteLine("Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on localhost:{runtime.Port}");
Console.WriteLine();

// Step 2 - Produce some orders
Console.WriteLine("Producing messages to 'orders' topic...");

await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

for (int i = 0; i < 10; i++)
{
    var orderId = $"ORD-{i}";
    var orderValue = $"order {orderId} for ${(i + 1) * 10}.00";
    await producer.ProduceAsync("orders", orderId, orderValue);
}

Console.WriteLine("  Produced 10 orders");
Console.WriteLine();

// Step 3 - Create a consumer with group ID
Console.WriteLine("Creating consumer with group 'order-processors'...");

await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "order-processors";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
    options.EnableAutoCommit = false;
});

consumer.PartitionsAssigned += (sender, args) =>
{
    Console.WriteLine($"Partitions assigned: [{string.Join(", ", args.Partitions)}]");
};

consumer.PartitionsRevoked += (sender, args) =>
{
    Console.WriteLine($"Partitions revoked: [{string.Join(", ", args.Partitions)}]");
};

Console.WriteLine("Joining consumer group...");
await consumer.SubscribeAsync(default, "orders");
Console.WriteLine("Consumer ready!");
Console.WriteLine();

// Step 4 - Process messages
Console.WriteLine("Processing orders...");

int processedCount = 0;
const int expectedOrders = 10;

while (processedCount < expectedOrders)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));

    if (result != null)
    {
        Console.WriteLine($"  Order {processedCount}: Processing {result.Value}");
        processedCount++;
    }
}

Console.WriteLine();

// Step 5 - Commit offsets
Console.WriteLine("Committing offsets...");
await consumer.CommitAsync();
Console.WriteLine("Offsets committed successfully!");
Console.WriteLine();
Console.WriteLine($"Processed {processedCount} orders");
Console.WriteLine();

// Step 6 - Clean shutdown
Console.WriteLine("Shutting down...");
Console.WriteLine("Goodbye!");
