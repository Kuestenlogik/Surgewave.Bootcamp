// Surgewave Bootcamp - Unit 20, Lesson 4: gRPC Clients
//
// In this lesson, you'll learn:
// - Understand gRPC service definitions
// - Use unary and streaming RPCs
// - Generate gRPC clients from proto files
// - Handle streaming responses
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 20.4 - gRPC Clients ===");
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

// TODO: Step 1 - Display gRPC service overview
// Console.WriteLine("--- gRPC Service Overview ---");
// Console.WriteLine();
// Console.WriteLine("ProducerService:");
// Console.WriteLine("  - Produce (unary): Single message produce");
// Console.WriteLine("  - ProduceBatch (client streaming): Batch produce");
// Console.WriteLine("  - ProduceStream (bidirectional): Real-time produce");
// Console.WriteLine();
// Console.WriteLine("ConsumerService:");
// Console.WriteLine("  - Consume (server streaming): Real-time consume");
// Console.WriteLine("  - Fetch (unary): Batch fetch");
// Console.WriteLine("  - Commit (unary): Commit offsets");
// Console.WriteLine("  - ListOffsets (unary): Get offsets");

// TODO: Step 2 - Simulate unary RPC (Produce)
// Console.WriteLine();
// Console.WriteLine("--- Simulating Unary RPC (Produce) ---");
// Console.WriteLine();
//
// var key = "order-1";
// var value = "{\"id\":1}";
//
// Console.WriteLine("Request:");
// Console.WriteLine($"  topic: \"{topic}\"");
// Console.WriteLine($"  partition: {partition}");
// Console.WriteLine($"  record: {{ key: \"{key}\", value: \"{value}\" }}");
// Console.WriteLine();
//
// var offset = await client.Messaging.SendAsync(topic, partition,
//     Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
//
// Console.WriteLine("Response:");
// Console.WriteLine($"  topic: \"{topic}\"");
// Console.WriteLine($"  partition: {partition}");
// Console.WriteLine($"  offset: {offset}");
// Console.WriteLine("  status: NONE");

// TODO: Step 3 - Produce more messages for streaming demo
// Console.WriteLine();
// Console.WriteLine("Producing additional messages...");
// for (int i = 2; i <= 3; i++)
// {
//     await client.Messaging.SendAsync(topic, partition,
//         Encoding.UTF8.GetBytes($"order-{i}"),
//         Encoding.UTF8.GetBytes($"{{\"id\":{i}}}"));
// }

// TODO: Step 4 - Simulate server streaming (Consume)
// Console.WriteLine();
// Console.WriteLine("--- Simulating Server Streaming (Consume) ---");
// Console.WriteLine();
// Console.WriteLine("Request:");
// Console.WriteLine($"  topic: \"{topic}\"");
// Console.WriteLine($"  partition: {partition}");
// Console.WriteLine("  offset: 0");
// Console.WriteLine("  max_records: 100");
// Console.WriteLine();
//
// var result = await client.Messaging.ReceiveAsync(topic, partition, 0, 1048576, maxWaitMs: 1000);
//
// Console.WriteLine("Streaming responses:");
// Console.WriteLine($"  Response 1: {result.Messages.Count} records (offsets 0-{result.Messages.Count - 1})");
// Console.WriteLine("  [End of stream]");

// TODO: Step 5 - Simulate client streaming (ProduceBatch)
// Console.WriteLine();
// Console.WriteLine("--- Simulating Client Streaming (ProduceBatch) ---");
// Console.WriteLine();
// Console.WriteLine("Sending 5 messages...");
//
// var batchOffsets = new List<long>();
// for (int i = 4; i <= 8; i++)
// {
//     var batchOffset = await client.Messaging.SendAsync(topic, partition,
//         Encoding.UTF8.GetBytes($"order-{i}"),
//         Encoding.UTF8.GetBytes($"{{\"id\":{i}}}"));
//     Console.WriteLine($"  Sent: order-{i}");
//     batchOffsets.Add(batchOffset);
// }
// Console.WriteLine("[Stream complete]");
// Console.WriteLine();
// Console.WriteLine("Batch response:");
// Console.WriteLine($"  total_records: {batchOffsets.Count}");
// Console.WriteLine($"  successful_records: {batchOffsets.Count}");
// Console.WriteLine("  failed_records: 0");

// TODO: Step 6 - Simulate bidirectional streaming (ProduceStream)
// Console.WriteLine();
// Console.WriteLine("--- Simulating Bidirectional Streaming (ProduceStream) ---");
// Console.WriteLine();
// Console.WriteLine("Interleaved send/receive:");
// for (int i = 9; i <= 11; i++)
// {
//     var streamOffset = await client.Messaging.SendAsync(topic, partition,
//         Encoding.UTF8.GetBytes($"order-{i}"),
//         Encoding.UTF8.GetBytes($"{{\"id\":{i}}}"));
//     Console.WriteLine($"  Sent: order-{i}, Received: offset {streamOffset}");
// }

// TODO: Step 7 - Performance comparison
// Console.WriteLine();
// Console.WriteLine("--- Performance Comparison ---");
// Console.WriteLine();
// Console.WriteLine("Characteristic        | REST        | gRPC");
// Console.WriteLine("---------------------|-------------|------------");
// Console.WriteLine("Payload (1 msg)      | ~200 bytes  | ~50 bytes");
// Console.WriteLine("Latency (unary)      | ~5ms        | ~1ms");
// Console.WriteLine("Streaming            | Poll-based  | Push-based");
// Console.WriteLine("Type safety          | Runtime     | Compile-time");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. gRPC uses Protobuf for smaller, faster messages");
Console.WriteLine("2. Four streaming patterns: unary, server, client, bidirectional");
Console.WriteLine("3. Type-safe clients generated from .proto files");
Console.WriteLine("4. HTTP/2 enables efficient multiplexing");
Console.WriteLine("5. Best for microservices and high-throughput scenarios");

Console.WriteLine();
Console.WriteLine("Lesson complete!");
