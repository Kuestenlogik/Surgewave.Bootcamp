# Knowledge Assessment: Lesson 20.4 - gRPC Clients

## Multiple Choice

### 1. Which gRPC streaming pattern is used for consuming messages where a single request produces multiple responses?
- [ ] A) Unary RPC
- [ ] B) Client streaming
- [ ] C) Server streaming
- [ ] D) Bidirectional streaming

<details>
<summary>Answer</summary>
**C)** Server streaming is used for consuming messages where the client sends a single ConsumeRequest and receives a stream of ConsumeResponse messages as they arrive.
</details>

### 2. What transport protocol does gRPC use?
- [ ] A) HTTP/1.1
- [ ] B) HTTP/2
- [ ] C) TCP directly
- [ ] D) WebSocket

<details>
<summary>Answer</summary>
**B)** gRPC uses HTTP/2, which provides features like multiplexing, header compression, and bidirectional streaming that enable efficient gRPC communication.
</details>

### 3. Which NuGet packages are required to generate and use gRPC clients in .NET?
- [ ] A) Grpc.Net.Client only
- [ ] B) Google.Protobuf only
- [ ] C) Grpc.Net.Client, Google.Protobuf, and Grpc.Tools
- [ ] D) System.Net.Grpc

<details>
<summary>Answer</summary>
**C)** All three packages are needed: Grpc.Net.Client for the gRPC runtime, Google.Protobuf for protobuf serialization, and Grpc.Tools for code generation from .proto files.
</details>

### 4. What is the advantage of gRPC's type safety compared to REST?
- [ ] A) Faster network transmission
- [ ] B) Compile-time validation of request/response schemas
- [ ] C) Better encryption support
- [ ] D) Simpler authentication

<details>
<summary>Answer</summary>
**B)** gRPC provides compile-time validation through generated code from protobuf definitions. Errors in request/response structure are caught at compile time rather than runtime.
</details>

### 5. Which streaming pattern would you use to send a batch of messages where you want acknowledgment after all messages are sent?
- [ ] A) Unary RPC
- [ ] B) Client streaming
- [ ] C) Server streaming
- [ ] D) Bidirectional streaming

<details>
<summary>Answer</summary>
**B)** Client streaming (ProduceBatch) allows sending multiple requests and receiving a single aggregated response after all messages are sent.
</details>

## True/False

### 6. gRPC services can be accessed directly from web browsers without any additional libraries.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Standard gRPC requires HTTP/2 features not available in browsers. Web browsers need gRPC-Web, a modified protocol that works with browser limitations.
</details>

### 7. In bidirectional streaming, the client must wait for the server to respond before sending the next message.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - In bidirectional streaming, both client and server can send messages at any time independently (full duplex). Neither side needs to wait for the other.
</details>

## Code Challenge

### 8. What's wrong with this gRPC consumer code?

```csharp
var consumer = new ConsumerService.ConsumerServiceClient(channel);
var call = consumer.Consume(new ConsumeRequest
{
    Topic = "orders",
    Partition = 0,
    Offset = 0,
    MaxRecords = 100
});

foreach (var message in call.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"Received: {message.Records[0].Value}");
}
```

<details>
<summary>Answer</summary>
There are two issues:

1. Missing `await` and `using` for the async streaming call
2. Accessing `message.Records[0]` without checking if records exist, and `Value` is bytes that need conversion

```csharp
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
    foreach (var record in message.Records)  // Iterate all records, handle empty case
    {
        Console.WriteLine($"Received: {record.Value.ToStringUtf8()}");  // Convert bytes to string
    }
}
```

The `using` ensures proper disposal of the call, `await foreach` properly handles the async stream, and iterating `Records` handles both empty and multi-record responses.
</details>

## Score
- 8/8: Excellent!
- 6-7/8: Good understanding
- 4-5/8: Review the lesson
- <4/8: Re-do the lesson
