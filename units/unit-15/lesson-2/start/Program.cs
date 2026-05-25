// Surgewave Bootcamp - Unit 20, Lesson 2: REST Produce/Consume
//
// In this lesson, you'll learn:
// - Produce messages via REST API
// - Consume messages via REST API (fetch-based)
// - Commit offsets via REST API
// - Handle errors and responses
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 20.2 - REST Produce/Consume ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
Console.WriteLine();

// Create a native client to simulate Gateway behavior
await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

var topic = "orders";
var partition = 0;
var consumerGroup = "my-consumer";

// TODO: Step 1 - Simulate REST API produce
// Show what a REST produce request looks like and produce messages
// Console.WriteLine("--- Simulating REST API Produce ---");
// Console.WriteLine();
// Console.WriteLine("POST /v3/topics/orders/records");
// Console.WriteLine("Request:");
//
// for (int i = 1; i <= 5; i++)
// {
//     var key = $"order-{i}";
//     var value = JsonSerializer.Serialize(new { id = i, item = $"widget-{i}" });
//
//     // Show REST request format
//     var request = new
//     {
//         record = new
//         {
//             key = Convert.ToBase64String(Encoding.UTF8.GetBytes(key)),
//             value = Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
//         },
//         partition = partition
//     };
//
//     if (i == 1)
//     {
//         Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }));
//         Console.WriteLine();
//     }
//
//     // Actual produce using native client (simulating what Gateway does)
//     var keyBytes = Encoding.UTF8.GetBytes(key);
//     var valueBytes = Encoding.UTF8.GetBytes(value);
//     var offset = await client.Messaging.SendAsync(topic, partition, keyBytes, valueBytes);
//
//     if (i == 1)
//     {
//         Console.WriteLine("Response:");
//         var response = new
//         {
//             topic = topic,
//             partition = partition,
//             offset = offset,
//             status = new { error_code = "NONE" }
//         };
//         Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
//     }
// }
//
// Console.WriteLine();
// Console.WriteLine("Produced 5 messages");

// TODO: Step 2 - Simulate REST API fetch
// Show what a REST fetch request looks like and fetch messages
// Console.WriteLine();
// Console.WriteLine("--- Simulating REST API Fetch ---");
// Console.WriteLine();
// Console.WriteLine("POST /v3/consumers/fetch");
// Console.WriteLine("Request:");
//
// var fetchRequest = new
// {
//     partitions = new[]
//     {
//         new { topic = topic, partition = partition, offset = 0L, max_bytes = 1048576 }
//     },
//     max_wait_ms = 5000
// };
// Console.WriteLine(JsonSerializer.Serialize(fetchRequest, new JsonSerializerOptions { WriteIndented = true }));
// Console.WriteLine();
//
// // Actual fetch using native client
// var result = await client.Messaging.ReceiveAsync(topic, partition, 0, 1048576, maxWaitMs: 5000);
//
// Console.WriteLine("Response:");
// var fetchResponse = new
// {
//     results = new[]
//     {
//         new
//         {
//             topic = topic,
//             partition = partition,
//             records = result.Messages.Select(m => new
//             {
//                 offset = m.Offset,
//                 key = m.Key != null ? Encoding.UTF8.GetString(m.Key) : null,
//                 value = Encoding.UTF8.GetString(m.Value)
//             }),
//             high_watermark = result.HighWatermark
//         }
//     }
// };
// Console.WriteLine(JsonSerializer.Serialize(fetchResponse, new JsonSerializerOptions { WriteIndented = true }));
// Console.WriteLine();
// Console.WriteLine($"Fetched {result.Messages.Count} messages");

// TODO: Step 3 - Simulate REST API offset commit
// Console.WriteLine();
// Console.WriteLine("--- Simulating REST API Offset Commit ---");
// Console.WriteLine();
// Console.WriteLine($"POST /v3/consumer-groups/{consumerGroup}/offsets");
// Console.WriteLine("Request:");
//
// var commitRequest = new
// {
//     offsets = new[]
//     {
//         new { topic = topic, partition = partition, offset = 5L }
//     }
// };
// Console.WriteLine(JsonSerializer.Serialize(commitRequest, new JsonSerializerOptions { WriteIndented = true }));
// Console.WriteLine();
//
// // Actual commit using native client
// await client.Groups.CommitOffsetAsync(consumerGroup, "", 0, topic, partition, 5);
//
// Console.WriteLine("Response:");
// var commitResponse = new
// {
//     results = new[]
//     {
//         new
//         {
//             topic = topic,
//             partition = partition,
//             status = new { error_code = "NONE" }
//         }
//     }
// };
// Console.WriteLine(JsonSerializer.Serialize(commitResponse, new JsonSerializerOptions { WriteIndented = true }));
// Console.WriteLine();
// Console.WriteLine("Committed offset 5");

// TODO: Step 4 - Simulate REST API fetch offsets
// Console.WriteLine();
// Console.WriteLine("--- Simulating REST API Fetch Offsets ---");
// Console.WriteLine();
// Console.WriteLine($"GET /v3/consumer-groups/{consumerGroup}/offsets");
// Console.WriteLine();
//
// var committedOffset = await client.Groups.FetchOffsetAsync(consumerGroup, topic, partition);
//
// Console.WriteLine("Response:");
// var fetchOffsetsResponse = new
// {
//     results = new[]
//     {
//         new
//         {
//             topic = topic,
//             partition = partition,
//             offset = committedOffset,
//             status = new { error_code = "NONE" }
//         }
//     }
// };
// Console.WriteLine(JsonSerializer.Serialize(fetchOffsetsResponse, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. REST API uses JSON for request/response");
Console.WriteLine("2. Binary data (keys/values) are Base64 encoded");
Console.WriteLine("3. Fetch is pull-based (polling model)");
Console.WriteLine("4. Offsets must be explicitly committed");
Console.WriteLine("5. Error codes indicate success or failure type");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
