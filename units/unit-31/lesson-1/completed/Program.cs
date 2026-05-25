// Surgewave Bootcamp - Unit 21, Lesson 1: Deduplication Concepts
// COMPLETED SOLUTION

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 21.1 - Deduplication Concepts ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "orders-topic";

// Step 1 - Message type with idempotency key is defined inline via anonymous types
// In production, you would use: public record OrderEvent(string IdempotencyKey, string OrderId, string Action, decimal Amount, DateTime Timestamp);

// Step 2 - Demonstrate idempotency key generation strategies
Console.WriteLine("\n--- Idempotency Key Strategies ---");

// Natural key from business identifier
var naturalKey = "order:ORD-001:created";
Console.WriteLine($"Natural key: {naturalKey}");

// Composite key with timestamp window (hourly granularity)
var compositeKey = $"payment-svc:order-123:charge:{DateTime.UtcNow:yyyy-MM-dd-HH}";
Console.WriteLine($"Composite key: {compositeKey}");

// Hash-based key from content
using var sha = SHA256.Create();
var content = "order:ORD-001:created:99.99";
var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
var hashKey = Convert.ToHexString(hash)[..32];
Console.WriteLine($"Hash-based key: {hashKey}");

// Step 3 - Simple deduplication checker using HashSet
var processedKeys = new HashSet<string>();

bool IsDuplicate(string idempotencyKey)
{
    return processedKeys.Contains(idempotencyKey);
}

bool TryMarkAsProcessed(string idempotencyKey)
{
    return processedKeys.Add(idempotencyKey);
}

// Step 4 - Create a producer and send messages (including duplicates)
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\n--- Simulating Message Production ---");

// Helper to create and serialize order events
string CreateOrderJson(string orderId, string action, decimal amount)
{
    var key = $"order:{orderId}:{action}";
    var order = new { IdempotencyKey = key, OrderId = orderId, Action = action, Amount = amount, Timestamp = DateTime.UtcNow };
    return JsonSerializer.Serialize(order);
}

var uniqueMessages = 0;
var duplicateMessages = 0;

// Send unique message
Console.WriteLine("Producing unique message: order:ORD-001:created");
await producer.ProduceAsync(topic, "ORD-001", CreateOrderJson("ORD-001", "created", 99.99m));
uniqueMessages++;

// Send DUPLICATE (same idempotency key - simulates producer retry)
Console.WriteLine("Producing DUPLICATE: order:ORD-001:created");
await producer.ProduceAsync(topic, "ORD-001", CreateOrderJson("ORD-001", "created", 99.99m));
duplicateMessages++;

// Send another unique message
Console.WriteLine("Producing unique message: order:ORD-002:created");
await producer.ProduceAsync(topic, "ORD-002", CreateOrderJson("ORD-002", "created", 149.99m));
uniqueMessages++;

// Send another unique message
Console.WriteLine("Producing unique message: order:ORD-003:updated");
await producer.ProduceAsync(topic, "ORD-003", CreateOrderJson("ORD-003", "updated", 199.99m));
uniqueMessages++;

// Send more duplicates
Console.WriteLine("Producing DUPLICATE: order:ORD-003:updated");
await producer.ProduceAsync(topic, "ORD-003", CreateOrderJson("ORD-003", "updated", 199.99m));
duplicateMessages++;

Console.WriteLine("Producing DUPLICATE: order:ORD-003:updated");
await producer.ProduceAsync(topic, "ORD-003", CreateOrderJson("ORD-003", "updated", 199.99m));
duplicateMessages++;

Console.WriteLine($"Produced {uniqueMessages + duplicateMessages} messages ({uniqueMessages} unique, {duplicateMessages} duplicates)");

// Step 5 - Create a consumer and process with deduplication
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "dedup-demo-group";
    options.EnableAutoCommit = false;
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

Console.WriteLine("\n--- Processing with Deduplication ---");

var duplicateCount = 0;
var processedCount = 0;
var totalMessages = uniqueMessages + duplicateMessages;

for (int i = 0; i < totalMessages; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    // Deserialize to get idempotency key
    var order = JsonSerializer.Deserialize<JsonElement>(result.Value);
    var idempotencyKey = order.GetProperty("IdempotencyKey").GetString()!;
    var orderId = order.GetProperty("OrderId").GetString();
    var action = order.GetProperty("Action").GetString();
    var amount = order.GetProperty("Amount").GetDecimal();

    // Check for duplicate BEFORE processing
    if (IsDuplicate(idempotencyKey))
    {
        duplicateCount++;
        Console.WriteLine($"DUPLICATE detected: {idempotencyKey} (skipping)");
        await consumer.CommitAsync(result);
        continue;
    }

    // Process the message (in real app, this would be business logic)
    Console.WriteLine($"Processing: {orderId} - {action} (${amount})");

    // Mark as processed AFTER successful processing
    TryMarkAsProcessed(idempotencyKey);
    processedCount++;

    await consumer.CommitAsync(result);
}

// Step 6 - Display deduplication statistics
Console.WriteLine("\n--- Deduplication Statistics ---");
Console.WriteLine($"Total messages received: {processedCount + duplicateCount}");
Console.WriteLine($"Unique messages processed: {processedCount}");
Console.WriteLine($"Duplicates detected: {duplicateCount}");
Console.WriteLine($"Deduplication rate: {(double)duplicateCount / (processedCount + duplicateCount):P2}");
Console.WriteLine($"Keys in dedup store: {processedKeys.Count}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Duplicates are normal in at-least-once delivery systems");
Console.WriteLine("2. Use idempotency keys to identify duplicate messages");
Console.WriteLine("3. Natural keys (business IDs) are preferred when available");
Console.WriteLine("4. Check for duplicates BEFORE processing");
Console.WriteLine("5. Store processed keys to detect future duplicates");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
