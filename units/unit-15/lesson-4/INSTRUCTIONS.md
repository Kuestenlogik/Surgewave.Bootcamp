# Lesson 20.4: Instructions

Follow these step-by-step instructions to understand gRPC clients.

## Step 1: Understand Service Definitions

Surgewave Gateway provides these gRPC services:

```csharp
// ProducerService operations
// - Produce: Single message produce (unary)
// - ProduceBatch: Multiple messages, one response (client streaming)
// - ProduceStream: Bidirectional streaming

// ConsumerService operations
// - Consume: Server streaming for real-time consumption
// - Fetch: Single batch fetch (unary)
// - Commit: Commit offsets (unary)
// - ListOffsets: Get earliest/latest offsets (unary)

// TopicService operations
// - CreateTopic, ListTopics, DescribeTopic, DeleteTopic

// ClusterService operations
// - GetCluster, ListClusters

// ConsumerGroupService operations
// - DescribeGroup, ListGroups, DeleteGroup

// TransactionService operations
// - InitProducerId, AddPartitionsToTxn, EndTransaction
```

## Step 2: Unary RPC (Produce)

Single request, single response:

```csharp
// Request
var produceRequest = new
{
    topic = "orders",
    partition = 0,
    record = new
    {
        key = new byte[] { /* key bytes */ },
        value = new byte[] { /* value bytes */ }
    },
    acks_required = true
};

// Response
var produceResponse = new
{
    topic = "orders",
    partition = 0,
    offset = 42,
    timestamp = 1706025600000,
    status = new { error_code = "NONE" }
};

// C# gRPC client usage:
// var response = await producer.ProduceAsync(request);
```

## Step 3: Server Streaming (Consume)

Single request, stream of responses:

```csharp
// Request
var consumeRequest = new
{
    topic = "orders",
    partition = 0,
    offset = 0,
    max_records = 100,
    max_wait_ms = 5000
};

// Server sends multiple responses over time
// Response 1: records 0-9
// Response 2: records 10-19
// Response N: ...

// C# gRPC client usage:
// using var call = consumer.Consume(request);
// await foreach (var response in call.ResponseStream.ReadAllAsync())
// {
//     foreach (var record in response.Records)
//     {
//         // Process record
//     }
// }
```

## Step 4: Client Streaming (ProduceBatch)

Multiple requests, single response:

```csharp
// Client sends multiple messages
// Message 1: { topic: "orders", record: { ... } }
// Message 2: { topic: "orders", record: { ... } }
// Message N: { topic: "orders", record: { ... } }

// Server responds with batch result
var batchResponse = new
{
    total_records = 100,
    successful_records = 98,
    failed_records = 2,
    responses = new[] { /* individual results */ }
};

// C# gRPC client usage:
// using var call = producer.ProduceBatch();
// foreach (var msg in messages)
// {
//     await call.RequestStream.WriteAsync(msg);
// }
// await call.RequestStream.CompleteAsync();
// var response = await call.ResponseAsync;
```

## Step 5: Bidirectional Streaming (ProduceStream)

Both sides can send at any time:

```csharp
// Client sends messages
// Server responds to each immediately

// C# gRPC client usage:
// using var call = producer.ProduceStream();
//
// // Read responses in background
// var readTask = Task.Run(async () =>
// {
//     await foreach (var response in call.ResponseStream.ReadAllAsync())
//     {
//         Console.WriteLine($"Produced at offset {response.Offset}");
//     }
// });
//
// // Send messages
// foreach (var msg in messages)
// {
//     await call.RequestStream.WriteAsync(msg);
// }
// await call.RequestStream.CompleteAsync();
// await readTask;
```

## Step 6: Project Setup for gRPC Client

```xml
<!-- Add to .csproj -->
<ItemGroup>
  <PackageReference Include="Grpc.Net.Client" Version="2.63.0" />
  <PackageReference Include="Google.Protobuf" Version="3.27.2" />
  <PackageReference Include="Grpc.Tools" Version="2.65.0" PrivateAssets="All" />
</ItemGroup>

<ItemGroup>
  <Protobuf Include="Protos\*.proto" GrpcServices="Client" />
</ItemGroup>
```

## Complete Solution

See `completed/Program.cs` for simulation of gRPC patterns.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 20.4 - gRPC Clients ===

Surgewave broker started on port 51234

--- gRPC Service Overview ---

ProducerService:
  - Produce (unary): Single message produce
  - ProduceBatch (client streaming): Batch produce
  - ProduceStream (bidirectional): Real-time produce

ConsumerService:
  - Consume (server streaming): Real-time consume
  - Fetch (unary): Batch fetch
  - Commit (unary): Commit offsets
  - ListOffsets (unary): Get offsets

--- Simulating Unary RPC (Produce) ---

Request:
  topic: "orders"
  partition: 0
  record: { key: "order-1", value: "{\"id\":1}" }

Response:
  topic: "orders"
  partition: 0
  offset: 0
  status: NONE

--- Simulating Server Streaming (Consume) ---

Request:
  topic: "orders"
  partition: 0
  offset: 0
  max_records: 100

Streaming responses:
  Response 1: 3 records (offsets 0-2)
  [End of stream]

--- Simulating Client Streaming (ProduceBatch) ---

Sending 5 messages...
  Sent: order-1
  Sent: order-2
  Sent: order-3
  Sent: order-4
  Sent: order-5
[Stream complete]

Batch response:
  total_records: 5
  successful_records: 5
  failed_records: 0

--- Simulating Bidirectional Streaming (ProduceStream) ---

Interleaved send/receive:
  Sent: order-6, Received: offset 5
  Sent: order-7, Received: offset 6
  Sent: order-8, Received: offset 7

--- Performance Comparison ---

Characteristic        | REST        | gRPC
---------------------|-------------|------------
Payload (1 msg)      | ~200 bytes  | ~50 bytes
Latency (unary)      | ~5ms        | ~1ms
Streaming            | Poll-based  | Push-based
Type safety          | Runtime     | Compile-time

--- Key Takeaways ---
1. gRPC uses Protobuf for smaller, faster messages
2. Four streaming patterns: unary, server, client, bidirectional
3. Type-safe clients generated from .proto files
4. HTTP/2 enables efficient multiplexing
5. Best for microservices and high-throughput scenarios

Lesson complete!
```

## Knowledge Check

1. What are the four gRPC streaming patterns?
2. How does gRPC achieve smaller payloads than REST?
3. When would you use client streaming vs server streaming?
4. What is needed to generate a gRPC client?
5. Why is HTTP/2 important for gRPC?

---

**Next:** [Lesson 20.5: Gateway Security](../lesson-5/README.md)
