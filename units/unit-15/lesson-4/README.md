# Lesson 20.4: gRPC Clients

> **Difficulty:** Advanced | **Duration:** 35 min | **Prerequisites:** Lesson 20.3

## Overview

Learn how to use Surgewave Gateway's gRPC API for high-performance messaging. This lesson covers Protobuf definitions, streaming RPCs, and client generation.

## Learning Objectives

- Understand gRPC service definitions
- Use unary and streaming RPCs
- Generate gRPC clients from proto files
- Handle streaming responses

## gRPC vs REST

| Feature | REST API | gRPC API |
|---------|---------|---------|
| Protocol | HTTP/1.1 (JSON) | HTTP/2 (Protobuf) |
| Payload size | Larger (text) | Smaller (binary) |
| Streaming | Limited (SSE) | Full bidirectional |
| Type safety | Runtime | Compile-time |
| Code generation | Optional | Built-in (protoc) |
| Browser support | Native | Via gRPC-Web |
| Debugging | Easy (human-readable) | Requires tools |
| Performance | Good | Excellent |

Choose gRPC when high throughput, streaming, type safety, or microservice-to-microservice communication is needed.

## gRPC Service Definitions

Surgewave Gateway exposes these Protobuf services:

```protobuf
// Producer Service
service ProducerService {
  // Unary RPC - single request, single response
  rpc Produce(ProduceRequest) returns (ProduceResponse);

  // Client streaming - multiple requests, single response
  rpc ProduceBatch(stream ProduceRequest) returns (ProduceBatchResponse);

  // Bidirectional streaming - multiple requests, multiple responses
  rpc ProduceStream(stream ProduceRequest) returns (stream ProduceResponse);
}

// Consumer Service
service ConsumerService {
  // Server streaming - single request, multiple responses
  rpc Consume(ConsumeRequest) returns (stream ConsumeResponse);

  // Unary RPCs
  rpc Fetch(FetchRequest) returns (FetchResponse);
  rpc Commit(CommitRequest) returns (CommitResponse);
  rpc ListOffsets(ListOffsetsRequest) returns (ListOffsetsResponse);
}
```

## Streaming Patterns

| Pattern | Flow | Surgewave Example |
|---------|------|--------------|
| Unary RPC | Client → Server → Client | `Produce` |
| Server Streaming | Client → Server → Client (N responses) | `Consume` |
| Client Streaming | Client (N requests) → Server → Client | `ProduceBatch` |
| Bidirectional Streaming | Full duplex, both send at any time | `ProduceStream` |

## Message Definitions

### ProduceRequest

```protobuf
message ProduceRequest {
  string cluster_id = 1;
  string topic = 2;
  int32 partition = 3;
  Record record = 4;
  bool acks_required = 5;
  string transactional_id = 6;
}

message Record {
  int64 offset = 1;
  int64 timestamp = 2;
  bytes key = 3;
  bytes value = 4;
  repeated Header headers = 5;
}

message Header {
  string key = 1;
  bytes value = 2;
}
```

### ConsumeRequest/Response

```protobuf
message ConsumeRequest {
  string topic = 1;
  int32 partition = 2;
  int64 offset = 3;
  int32 max_records = 4;
  int32 max_wait_ms = 5;
  string consumer_group = 6;
}

message ConsumeResponse {
  string topic = 1;
  int32 partition = 2;
  repeated Record records = 3;
  int64 high_watermark = 4;
  ResponseStatus status = 5;
}
```

## Client Generation

Generate C# clients from proto files:

```bash
# Install tools
dotnet tool install -g Grpc.Tools

# Add NuGet packages to project
dotnet add package Grpc.Net.Client
dotnet add package Google.Protobuf
dotnet add package Grpc.Tools

# Proto files are compiled automatically when referenced in .csproj
```

## Using the gRPC Client

```csharp
// Create channel
using var channel = GrpcChannel.ForAddress("http://localhost:8080");

// Create client
var producer = new ProducerService.ProducerServiceClient(channel);

// Unary call
var response = await producer.ProduceAsync(new ProduceRequest
{
    Topic = "orders",
    Partition = 0,
    Record = new Record
    {
        Key = ByteString.CopyFromUtf8("key-1"),
        Value = ByteString.CopyFromUtf8("{\"id\":1}")
    }
});

// Server streaming
var consumer = new ConsumerService.ConsumerServiceClient(channel);
using var call = consumer.Consume(new ConsumeRequest
{
    Topic = "orders",
    Partition = 0,
    Offset = 0,
    MaxRecords = 100
});

await foreach (var message in call.ResponseStream.ReadAllAsync())
{
    foreach (var record in message.Records)
    {
        Console.WriteLine($"Received: {record.Value.ToStringUtf8()}");
    }
}
```

## Exercise

In this exercise, you'll:

1. Understand gRPC service definitions
2. Simulate unary and streaming RPC patterns
3. See how messages are serialized
4. Compare gRPC with REST performance characteristics

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
