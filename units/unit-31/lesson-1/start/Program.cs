// Surgewave Bootcamp - Unit 21, Lesson 1: Deduplication Concepts
//
// In this lesson, you'll learn:
// - Why message duplicates occur in distributed systems
// - How to design idempotency keys
// - Basic deduplication detection patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// TODO: Step 1 - Define a message type with idempotency key
// public record OrderEvent(
//     string IdempotencyKey,
//     string OrderId,
//     string Action,
//     decimal Amount,
//     DateTime Timestamp
// );

// TODO: Step 2 - Create idempotency key generation methods
// Console.WriteLine("\n--- Idempotency Key Strategies ---");
//
// // Natural key from business identifier
// var naturalKey = $"order:ORD-001:created";
// Console.WriteLine($"Natural key: {naturalKey}");
//
// // Composite key with timestamp window
// var compositeKey = $"payment-svc:order-123:charge:{DateTime.UtcNow:yyyy-MM-dd-HH}";
// Console.WriteLine($"Composite key: {compositeKey}");
//
// // Hash-based key from content
// using var sha = SHA256.Create();
// var content = "order:ORD-001:created:99.99";
// var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
// var hashKey = Convert.ToHexString(hash)[..32];
// Console.WriteLine($"Hash-based key: {hashKey}");

// TODO: Step 3 - Implement a simple deduplication checker
// var processedKeys = new HashSet<string>();
//
// bool IsDuplicate(string idempotencyKey)
// {
//     return processedKeys.Contains(idempotencyKey);
// }
//
// bool TryMarkAsProcessed(string idempotencyKey)
// {
//     return processedKeys.Add(idempotencyKey);
// }

// TODO: Step 4 - Create a producer and send messages (including duplicates)
// await using var producer = new SurgewaveProducer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// Console.WriteLine("\n--- Simulating Message Production ---");
//
// // Helper to create and serialize order events
// string CreateOrderJson(string orderId, string action, decimal amount)
// {
//     var key = $"order:{orderId}:{action}";
//     var order = new { IdempotencyKey = key, OrderId = orderId, Action = action, Amount = amount, Timestamp = DateTime.UtcNow };
//     return JsonSerializer.Serialize(order);
// }
//
// // Send unique message
// Console.WriteLine("Producing unique message: order:ORD-001:created");
// await producer.ProduceAsync(topic, "ORD-001", CreateOrderJson("ORD-001", "created", 99.99m));
//
// // Send DUPLICATE (same idempotency key)
// Console.WriteLine("Producing DUPLICATE: order:ORD-001:created");
// await producer.ProduceAsync(topic, "ORD-001", CreateOrderJson("ORD-001", "created", 99.99m));
//
// // Send more unique messages
// Console.WriteLine("Producing unique message: order:ORD-002:created");
// await producer.ProduceAsync(topic, "ORD-002", CreateOrderJson("ORD-002", "created", 149.99m));

// TODO: Step 5 - Create a consumer and process with deduplication
// await using var consumer = new SurgewaveConsumer<string, string>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
//     options.GroupId = "dedup-demo-group";
//     options.EnableAutoCommit = false;
//     options.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// await consumer.SubscribeAsync(default, topic);
//
// Console.WriteLine("\n--- Processing with Deduplication ---");
//
// var duplicateCount = 0;
// var processedCount = 0;
//
// for (int i = 0; i < 3; i++)  // Process 3 messages
// {
//     var result = await consumer.ConsumeAsync();
//     if (result == null) continue;
//
//     // Deserialize to get idempotency key
//     var order = JsonSerializer.Deserialize<JsonElement>(result.Value);
//     var idempotencyKey = order.GetProperty("IdempotencyKey").GetString()!;
//     var orderId = order.GetProperty("OrderId").GetString();
//     var action = order.GetProperty("Action").GetString();
//     var amount = order.GetProperty("Amount").GetDecimal();
//
//     // Check for duplicate
//     if (IsDuplicate(idempotencyKey))
//     {
//         duplicateCount++;
//         Console.WriteLine($"DUPLICATE detected: {idempotencyKey} (skipping)");
//         await consumer.CommitAsync(result);
//         continue;
//     }
//
//     // Process the message
//     Console.WriteLine($"Processing: {orderId} - {action} (${amount})");
//
//     // Mark as processed
//     TryMarkAsProcessed(idempotencyKey);
//     processedCount++;
//
//     await consumer.CommitAsync(result);
// }

// TODO: Step 6 - Display deduplication statistics
// Console.WriteLine("\n--- Deduplication Statistics ---");
// Console.WriteLine($"Total messages received: {processedCount + duplicateCount}");
// Console.WriteLine($"Unique messages processed: {processedCount}");
// Console.WriteLine($"Duplicates detected: {duplicateCount}");
// if (processedCount + duplicateCount > 0)
// {
//     Console.WriteLine($"Deduplication rate: {(double)duplicateCount / (processedCount + duplicateCount):P2}");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Duplicates are normal in at-least-once delivery systems");
Console.WriteLine("2. Use idempotency keys to identify duplicate messages");
Console.WriteLine("3. Natural keys (business IDs) are preferred when available");
Console.WriteLine("4. Check for duplicates BEFORE processing");
Console.WriteLine("5. Store processed keys to detect future duplicates");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
