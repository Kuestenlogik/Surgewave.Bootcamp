// Unit 5 Project: Kafka to Surgewave Migration Tool - COMPLETED
// Migrate messages from Kafka-style topics to Surgewave using the compatibility layer

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Kafka to Surgewave Migration Tool ===");
Console.WriteLine();

// Configuration
const string SourceTopic = "legacy-orders";
const string TargetTopic = "surgewave-orders";
const int MessageCount = 100;

// Step 1 - Start embedded Surgewave broker
Console.WriteLine("[Setup] Starting embedded Surgewave broker...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine("[Setup] Broker ready on port 9092");
Console.WriteLine();

// Step 2 - Create Kafka-compatible producer
Console.WriteLine("[Phase 1] Creating legacy Kafka-style messages...");
Console.WriteLine($"  Producing {MessageCount} orders to '{SourceTopic}' topic...");

await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    // opt.Acks = Acks.All; // TODO: Acks not yet available on SurgewaveProducerOptions
});

// Generate and produce sample orders
var random = new Random(42); // Fixed seed for reproducibility
var orders = new List<LegacyOrderMessage>();

for (int i = 1; i <= MessageCount; i++)
{
    var order = new LegacyOrderMessage(
        OrderId: $"order-{i:D3}",
        CustomerId: $"customer-{random.Next(1, 20):D3}",
        Amount: Math.Round((decimal)(random.NextDouble() * 1000), 2),
        Status: random.Next(3) switch { 0 => "pending", 1 => "shipped", _ => "delivered" },
        Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    );
    orders.Add(order);

    // Produce each order to SourceTopic
    var json = JsonSerializer.Serialize(order);
    await producer.ProduceAsync(SourceTopic, order.OrderId, json);

    if (i % 20 == 0)
    {
        Console.WriteLine($"  Produced: order-001 ... order-{i:D3}");
    }
}

// Flush producer to ensure all messages are sent
await producer.FlushAsync();

Console.WriteLine($"  Total produced: {MessageCount} messages");
Console.WriteLine();

// Step 3 - Create Kafka-compatible consumer and migrate messages
Console.WriteLine("[Phase 2] Migrating messages to Surgewave...");
Console.WriteLine($"  Consuming from '{SourceTopic}'...");

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "migration-tool";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    opt.EnableAutoCommit = false;
});

var stopwatch = Stopwatch.StartNew();
var migratedCount = 0;
var failedCount = 0;

// Subscribe to SourceTopic
consumer.Subscribe(SourceTopic);

Console.WriteLine("  Validating and transforming...");
Console.WriteLine($"  Producing to '{TargetTopic}'...");

// Consume messages and produce to target topic
while (migratedCount + failedCount < MessageCount)
{
    var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
    if (result == null) break;

    try
    {
        // Validate message
        var order = JsonSerializer.Deserialize<LegacyOrderMessage>(result.Value);
        if (order == null) throw new InvalidOperationException("Invalid message");

        // Produce to target topic
        await producer.ProduceAsync(TargetTopic, result.Key, result.Value);
        migratedCount++;

        // Show progress every 25%
        if (migratedCount % 25 == 0)
        {
            Console.WriteLine($"  Progress: {migratedCount}/{MessageCount} ({migratedCount * 100 / MessageCount}%)");
        }
    }
    catch (Exception ex)
    {
        failedCount++;
        Console.WriteLine($"  Error migrating message: {ex.Message}");
    }
}

// Commit offsets after migration
await consumer.CommitAsync();
await producer.FlushAsync();

stopwatch.Stop();
Console.WriteLine();

// Step 4 - Validate migration
Console.WriteLine("[Phase 3] Validating migration...");

// Count messages in target topic using a new consumer
await using var validationConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = "migration-validator";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

validationConsumer.Subscribe(TargetTopic);

var targetCount = 0;
while (true)
{
    var msg = await validationConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (msg == null) break;
    targetCount++;
}

Console.WriteLine($"  Source topic: {MessageCount} messages (produced)");
Console.WriteLine($"  Target topic: {targetCount} messages (migrated)");

var validationPassed = targetCount == migratedCount && failedCount == 0;
Console.WriteLine($"  Validation: {(validationPassed ? "PASSED" : "FAILED")}");
Console.WriteLine();

// Step 5 - Generate migration report
var stats = new MigrationStats(
    TotalMessages: MessageCount,
    MigratedMessages: migratedCount,
    FailedMessages: failedCount,
    Duration: stopwatch.Elapsed
);

var throughput = migratedCount / Math.Max(0.001, stats.Duration.TotalSeconds);

Console.WriteLine("=== Migration Report ===");
Console.WriteLine($"Source Topic:      {SourceTopic}");
Console.WriteLine($"Target Topic:      {TargetTopic}");
Console.WriteLine($"Messages Migrated: {stats.MigratedMessages}");
Console.WriteLine($"Failed:            {stats.FailedMessages}");
Console.WriteLine($"Duration:          {stats.Duration.TotalSeconds:F2}s");
Console.WriteLine($"Throughput:        {throughput:F1} msg/sec");
Console.WriteLine($"Status:            {(validationPassed ? "SUCCESS" : "FAILED")}");
Console.WriteLine();
Console.WriteLine("=== Migration Complete ===");

// Data types
public record LegacyOrderMessage(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Status,
    long Timestamp
);

public record MigrationStats(
    int TotalMessages,
    int MigratedMessages,
    int FailedMessages,
    TimeSpan Duration
);
