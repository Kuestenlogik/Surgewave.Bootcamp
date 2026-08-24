# Lesson 20.1: Instructions

Follow these step-by-step instructions to explore Gateway architecture.

## Step 1: Understand the Gateway Configuration

The Gateway configuration defines which clusters are available:

```csharp
// Gateway configuration structure
var gatewayConfig = new
{
    DefaultCluster = "surgewave-cluster",
    Clusters = new Dictionary<string, object>
    {
        ["surgewave-cluster"] = new { BootstrapServers = "localhost:9092" }
    },
    ConnectionTimeoutMs = 30000,
    RequestTimeoutMs = 30000
};
```

## Step 2: Start an Embedded Surgewave Broker

First, create and start the Surgewave broker that the Gateway will connect to:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)  // Dynamic port
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
```

## Step 3: Understand REST Endpoint Structure

Surgewave Gateway follows the Confluent REST Proxy v3 API structure:

```
Base paths:
  /v3/clusters/{cluster_id}/...  - Multi-cluster explicit routing
  /v3/...                        - Default cluster routing

Topic endpoints:
  GET    /v3/topics               - List all topics
  POST   /v3/topics               - Create a topic
  GET    /v3/topics/{topic}       - Describe a topic
  DELETE /v3/topics/{topic}       - Delete a topic

Producer endpoints:
  POST   /v3/topics/{topic}/records  - Produce a record

Consumer endpoints:
  POST   /v3/consumers/fetch                      - Fetch records
  POST   /v3/consumer-groups/{group}/offsets      - Commit offsets
  GET    /v3/consumer-groups/{group}/offsets      - Fetch offsets

Cluster endpoints:
  GET    /v3/clusters             - List clusters
  GET    /v3/clusters/{id}        - Get cluster info
```

## Step 4: Understand gRPC Service Structure

The Gateway exposes these gRPC services:

```protobuf
// ProducerService - Produce messages
service ProducerService {
  rpc Produce(ProduceRequest) returns (ProduceResponse);
  rpc ProduceBatch(stream ProduceRequest) returns (ProduceBatchResponse);
  rpc ProduceStream(stream ProduceRequest) returns (stream ProduceResponse);
}

// ConsumerService - Consume messages
service ConsumerService {
  rpc Consume(ConsumeRequest) returns (stream ConsumeResponse);
  rpc Fetch(FetchRequest) returns (FetchResponse);
  rpc Commit(CommitRequest) returns (CommitResponse);
  rpc ListOffsets(ListOffsetsRequest) returns (ListOffsetsResponse);
}

// TopicService - Manage topics
service TopicService {
  rpc CreateTopic(CreateTopicRequest) returns (CreateTopicResponse);
  rpc ListTopics(ListTopicsRequest) returns (ListTopicsResponse);
  rpc DescribeTopic(DescribeTopicRequest) returns (DescribeTopicResponse);
  rpc DeleteTopic(DeleteTopicRequest) returns (DeleteTopicResponse);
}
```

## Step 5: Explore Multi-Cluster Routing

The Gateway supports routing to multiple Surgewave clusters:

```csharp
// Default cluster routing (uses DefaultCluster from config)
var defaultUrl = "/v3/topics/my-topic/records";

// Explicit cluster routing (specifies cluster_id in URL)
var explicitUrl = "/v3/clusters/production/topics/my-topic/records";

// WebSocket routing
var defaultWs = "ws://localhost:8080/ws";
var explicitWs = "ws://localhost:8080/ws/clusters/staging";
```

## Step 6: Understand Gateway Benefits

The Gateway provides several advantages:

1. **Protocol Translation**: Converts HTTP/gRPC to Surgewave native protocol
2. **Language Agnostic**: Any language with HTTP support can use Surgewave
3. **Firewall Friendly**: Uses standard HTTP/HTTPS ports
4. **Load Balancing**: Multiple Gateway instances can be load balanced
5. **Multi-Cluster**: Single Gateway can route to multiple Surgewave clusters
6. **WebSocket Streaming**: Real-time bidirectional communication

## Complete Solution

See `completed/Program.cs` for a demonstration of Gateway architecture concepts.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 20.1 - Gateway Architecture ===

Surgewave broker started on port 51234

--- Gateway Architecture Overview ---

Gateway provides three access methods:
  1. REST API (JSON over HTTP)
     - Easy to use from any language
     - Human-readable for debugging
     - Best for: Web clients, scripts, simple integrations

  2. gRPC API (Protobuf over HTTP/2)
     - High performance, strongly typed
     - Bidirectional streaming support
     - Best for: Microservices, high-throughput clients

  3. WebSocket (JSON over WebSocket)
     - Real-time bidirectional communication
     - Persistent connections
     - Best for: Live dashboards, real-time apps

--- REST API Endpoints (Confluent v3 Compatible) ---

Topic Management:
  GET    /v3/topics               - List topics
  POST   /v3/topics               - Create topic
  GET    /v3/topics/{topic}       - Describe topic
  DELETE /v3/topics/{topic}       - Delete topic

Producing:
  POST   /v3/topics/{topic}/records - Produce message

Consuming:
  POST   /v3/consumers/fetch      - Fetch messages
  POST   /v3/consumer-groups/{g}/offsets - Commit offsets
  GET    /v3/consumer-groups/{g}/offsets - Get offsets

--- Multi-Cluster Support ---

Single cluster (default):
  POST /v3/topics/orders/records

Multi-cluster (explicit):
  POST /v3/clusters/production/topics/orders/records
  POST /v3/clusters/staging/topics/orders/records

--- Key Takeaways ---
1. Gateway bridges HTTP/gRPC to Surgewave native protocol
2. REST API is Confluent v3 compatible
3. gRPC provides streaming and high performance
4. WebSocket enables real-time applications
5. Multi-cluster routing from single Gateway

Lesson complete!
```

## Knowledge Check

1. What are the three access methods provided by the Gateway?
2. When would you choose REST over gRPC?
3. How does multi-cluster routing work?
4. What is the purpose of JSON transcoding?
5. What are the main Gateway services?

---

**Next:** [Lesson 20.2: REST Produce/Consume](../lesson-2/README.md)
