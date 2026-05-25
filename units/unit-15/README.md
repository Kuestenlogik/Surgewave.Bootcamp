# Unit 15: gRPC/REST Gateway

Master Surgewave's HTTP/gRPC gateway to enable web clients and REST API integration for your messaging applications.

## Learning Objectives

By the end of this unit, you will be able to:

- Understand the Gateway architecture and its REST/gRPC dual nature
- Produce and consume messages via HTTP REST endpoints
- Implement real-time streaming with WebSocket connections
- Build gRPC clients using Surgewave's protobuf definitions
- Configure Gateway security with authentication, rate limiting, and CORS

## Lessons

| Lesson | Topic | Description |
|--------|-------|-------------|
| [20.1](lesson-1/README.md) | Gateway Architecture | REST vs gRPC, Confluent v3 API compatibility, routing |
| [20.2](lesson-2/README.md) | REST Produce/Consume | HTTP endpoints for producing and consuming messages |
| [20.3](lesson-3/README.md) | WebSocket Streaming | Real-time bidirectional communication, subscriptions |
| [20.4](lesson-4/README.md) | gRPC Clients | Protobuf definitions, streaming RPCs, client generation |
| [20.5](lesson-5/README.md) | Gateway Security | Authentication, rate limiting, CORS configuration |

## Prerequisites

- Completed Unit 1: Surgewave Fundamentals
- Completed Unit 2: Producer & Consumer Basics
- Basic understanding of HTTP/REST APIs
- Familiarity with JSON data format

## Gateway Architecture Overview

```mermaid
graph TD
    RC["REST Client (curl, fetch)"]
    GC["gRPC Client (Generated)"]
    WC[WebSocket Client]

    subgraph GW [Surgewave Gateway]
        REST["REST Endpoints (JSON Transcoding)<br/>POST /v3/topics, GET /v3/clusters"]
        GRPC["gRPC Services (Proto Streaming)<br/>ProducerService, ConsumerService, TopicService"]
        WS["WebSocket Handler<br/>/ws subscribe, produce"]
        CR["Cluster Registry (Multi-cluster Support)"]
        REST --> CR
        GRPC --> CR
        WS --> CR
    end

    subgraph Brokers [Surgewave Broker(s)]
        TO["Topic: orders (3 partitions)"]
        TE["Topic: events (2 partitions)"]
        TL["Topic: logs (1 partition)"]
    end

    RC --> REST
    GC --> GRPC
    WC --> WS
    CR --> Brokers
```

## Protocol Comparison

| Protocol | Pros | Cons |
|----------|------|------|
| REST (HTTP/JSON) | Universal compatibility, easy to debug (curl/browser), firewall-friendly, no client generation | Higher overhead (JSON), no streaming (request/response), higher latency, larger payloads |
| gRPC (HTTP/2 + Protobuf) | Binary protocol (compact), bidirectional streaming, strong typing via protobuf, lower latency | Requires client generation, less debuggable, may need HTTP/2 support, more complex setup |
| WebSocket | Real-time bidirectional, low overhead after connect, browser native support, great for subscriptions | Connection management overhead, stateful (harder to scale), firewall issues possible, need reconnect logic |

## Confluent v3 API Compatibility

| Category | Method | Route | Description |
|----------|--------|-------|-------------|
| Cluster | GET | `/v3/clusters` | List clusters |
| Cluster | GET | `/v3/clusters/{cluster_id}` | Get cluster info |
| Topic | GET | `/v3/clusters/{id}/topics` | List topics |
| Topic | POST | `/v3/clusters/{id}/topics` | Create topic |
| Topic | GET | `/v3/clusters/{id}/topics/{name}` | Get topic |
| Topic | DELETE | `/v3/clusters/{id}/topics/{name}` | Delete topic |
| Producer | POST | `/v3/clusters/{id}/topics/{name}/records` | Produce message |
| Producer | POST | `/v3/topics/{name}/records` | Produce (simplified) |
| Consumer | POST | `/v3/consumers/{group}` | Create consumer |
| Consumer | POST | `/v3/consumers/{group}/subscribe` | Subscribe to topics |
| Consumer | GET | `/v3/consumers/{group}/records` | Fetch records |
| Consumer | POST | `/v3/consumers/{group}/offsets` | Commit offsets |
| Consumer Groups | GET | `/v3/clusters/{id}/consumer-groups` | List groups |
| Consumer Groups | GET | `/v3/clusters/{id}/consumer-groups/{name}` | Describe group |

## WebSocket Message Protocol

**Client → Server:**

| Message Type | Description |
|-------------|-------------|
| `ping` | Heartbeat check |
| `subscribe` | Subscribe to topic with offset |
| `unsubscribe` | Unsubscribe from topic/partition |
| `produce` | Send message to topic |
| `produce_batch` | Send multiple messages |
| `commit` | Commit consumer offsets |
| `admin` | Admin operations (list topics, etc.) |

**Server → Client:**

| Message Type | Description |
|-------------|-------------|
| `pong` | Heartbeat response |
| `subscribed` | Subscription confirmed |
| `unsubscribed` | Unsubscription confirmed |
| `message` | Message from subscription |
| `messages` | Batch of messages |
| `produced` | Produce acknowledged |
| `committed` | Offset commit confirmed |
| `admin_result` | Admin operation result |
| `error` | Error response |

## Key Concepts

### Multi-Cluster Support

```csharp
// Gateway can connect to multiple Surgewave clusters
// Use cluster_id in routes to target specific clusters

// Default cluster (when cluster_id omitted):
// POST /v3/topics/orders/records

// Specific cluster:
// POST /v3/clusters/production/topics/orders/records
```

### JSON Transcoding

```csharp
// gRPC services are automatically exposed as REST endpoints
// via gRPC JSON transcoding

// gRPC call:
var response = await client.ProduceAsync(new ProduceRequest { ... });

// Equivalent REST call:
POST /v3/topics/orders/records
Content-Type: application/json
{
  "record": {
    "key": "base64-encoded-key",
    "value": "base64-encoded-value"
  }
}
```

### WebSocket Subscriptions

```csharp
// Connect to WebSocket endpoint
var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:8082/ws"), ct);

// Subscribe to topic
var subscribe = new {
    type = "subscribe",
    id = "req-1",
    payload = new {
        topic = "orders",
        partition = 0,
        offset = 0
    }
};
await ws.SendAsync(JsonSerializer.SerializeToUtf8Bytes(subscribe), ...);

// Receive messages
while (true) {
    var result = await ws.ReceiveAsync(buffer, ct);
    var message = JsonSerializer.Deserialize<WebSocketMessage>(buffer);
    // Process message...
}
```

## Gateway Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| DefaultCluster | surgewave-cluster | Default cluster when none specified |
| ConnectionTimeoutMs | 30000 | Timeout for connecting to broker |
| RequestTimeoutMs | 30000 | Timeout for individual requests |
| WebSocket.Enabled | true | Enable WebSocket support |
| WebSocket.HeartbeatIntervalMs | 30000 | Ping/pong interval |
| WebSocket.MaxSubscriptionsPerSession | 100 | Max subscriptions per connection |

---

**Ready?** [Start Lesson 20.1: Gateway Architecture](lesson-1/README.md)
