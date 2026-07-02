// Surgewave Bootcamp - Unit 20, Lesson 3: WebSocket Streaming
//
// In this lesson, you'll learn:
// - Connect to Surgewave via WebSocket
// - Subscribe to topics for real-time streaming
// - Produce messages via WebSocket
// - Manage subscriptions and sessions
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 20.3 - WebSocket Streaming ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
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
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

// TODO: Step 1 - Simulate WebSocket connection
// Console.WriteLine("--- WebSocket Connection ---");
// Console.WriteLine();
// Console.WriteLine("Connecting to: ws://gateway:8080/ws");
// var sessionId = $"session-{Guid.NewGuid():N}".Substring(0, 20);
// Console.WriteLine($"Session created with ID: {sessionId}");

// TODO: Step 2 - Simulate subscription flow
// Console.WriteLine();
// Console.WriteLine("--- Subscription Flow ---");
// Console.WriteLine();
//
// var subscribeRequest = new
// {
//     type = "subscribe",
//     payload = new { topic = topic, partition = partition, offset = -1 },
//     correlation_id = "sub-1"
// };
// Console.WriteLine("Sending subscribe request:");
// Console.WriteLine(JsonSerializer.Serialize(subscribeRequest, jsonOptions));
// Console.WriteLine();
//
// var subscriptionId = $"sub-{Guid.NewGuid():N}".Substring(0, 10);
// var subscribeResponse = new
// {
//     type = "subscribed",
//     payload = new { topic = topic, partition = partition, subscription_id = subscriptionId },
//     correlation_id = "sub-1"
// };
// Console.WriteLine("Received subscription confirmation:");
// Console.WriteLine(JsonSerializer.Serialize(subscribeResponse, jsonOptions));

// TODO: Step 3 - Simulate producing via WebSocket
// Console.WriteLine();
// Console.WriteLine("--- Producing via WebSocket ---");
// Console.WriteLine();
//
// for (int i = 1; i <= 3; i++)
// {
//     var key = $"order-{i}";
//     var value = JsonSerializer.Serialize(new { id = i, item = $"widget-{i}" });
//
//     var produceRequest = new
//     {
//         type = "produce",
//         payload = new { topic = topic, key = key, value = value },
//         correlation_id = $"prod-{i}"
//     };
//
//     if (i == 1)
//     {
//         Console.WriteLine("Sending produce request:");
//         Console.WriteLine(JsonSerializer.Serialize(produceRequest, jsonOptions));
//         Console.WriteLine();
//     }
//
//     // Actual produce
//     var keyBytes = Encoding.UTF8.GetBytes(key);
//     var valueBytes = Encoding.UTF8.GetBytes(value);
//     var offset = await client.Messaging.SendAsync(topic, partition, keyBytes, valueBytes);
//
//     if (i == 1)
//     {
//         var produceResponse = new
//         {
//             type = "produced",
//             payload = new { topic = topic, partition = partition, offset = offset },
//             correlation_id = $"prod-{i}"
//         };
//         Console.WriteLine("Received produce confirmation:");
//         Console.WriteLine(JsonSerializer.Serialize(produceResponse, jsonOptions));
//     }
// }
//
// Console.WriteLine();
// Console.WriteLine("Produced 3 messages");

// TODO: Step 4 - Simulate receiving streamed messages
// Console.WriteLine();
// Console.WriteLine("--- Receiving Streamed Messages ---");
// Console.WriteLine();
//
// var result = await client.Messaging.ReceiveAsync(topic, partition, 0, 1048576, maxWaitMs: 1000);
//
// foreach (var msg in result.Messages.Take(2))
// {
//     var messageEvent = new
//     {
//         type = "message",
//         payload = new
//         {
//             topic = topic,
//             partition = partition,
//             offset = msg.Offset,
//             key = msg.Key != null ? Encoding.UTF8.GetString(msg.Key) : null,
//             value = Encoding.UTF8.GetString(msg.Value),
//             timestamp = msg.Timestamp
//         }
//     };
//     Console.WriteLine("Received message push:");
//     Console.WriteLine(JsonSerializer.Serialize(messageEvent, jsonOptions));
//     Console.WriteLine();
// }

// TODO: Step 5 - Simulate admin operations
// Console.WriteLine("--- Admin Operations ---");
// Console.WriteLine();
//
// var listTopicsRequest = new { type = "list_topics", correlation_id = "admin-1" };
// Console.WriteLine("Sending: " + JsonSerializer.Serialize(listTopicsRequest));
//
// var topicsResponse = new
// {
//     type = "topics",
//     payload = new { topics = new[] { topic } },
//     correlation_id = "admin-1"
// };
// Console.WriteLine("Response: " + JsonSerializer.Serialize(topicsResponse, jsonOptions));

// TODO: Step 6 - Simulate heartbeat
// Console.WriteLine();
// Console.WriteLine("--- Heartbeat ---");
// Console.WriteLine();
// Console.WriteLine("Sent: { \"type\": \"heartbeat\" }");
// Console.WriteLine("Received: { \"type\": \"heartbeat_ack\" }");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. WebSocket provides real-time bidirectional streaming");
Console.WriteLine("2. Subscribe once, receive messages as they arrive");
Console.WriteLine("3. Produce and consume over same connection");
Console.WriteLine("4. Heartbeat keeps connection alive");
Console.WriteLine("5. Use for dashboards and real-time applications");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
