# Lesson 20.3: Instructions

Follow these step-by-step instructions to understand WebSocket streaming.

## Step 1: Understand WebSocket Message Format

All WebSocket messages use JSON with a common structure:

```csharp
// WebSocket message structure
public record WebSocketMessage
{
    public string Type { get; init; } = "";
    public object? Payload { get; init; }
    public string? CorrelationId { get; init; }
}
```

## Step 2: Subscribe to a Topic

Send a subscribe message to start receiving messages:

```csharp
// Subscribe request
var subscribeRequest = new
{
    type = "subscribe",
    payload = new
    {
        topic = "orders",
        partition = 0,
        offset = -1  // -1 = latest, -2 = earliest
    },
    correlation_id = "sub-1"
};

// Expected response
var subscribeResponse = new
{
    type = "subscribed",
    payload = new
    {
        topic = "orders",
        partition = 0,
        subscription_id = "unique-id"
    },
    correlation_id = "sub-1"
};
```

## Step 3: Receive Streamed Messages

Once subscribed, messages are pushed automatically:

```csharp
// Server pushes messages as they arrive
var messageEvent = new
{
    type = "message",
    payload = new
    {
        topic = "orders",
        partition = 0,
        offset = 42,
        key = "order-1",
        value = "{\"id\":1}",
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    }
};

// Batch delivery for high throughput
var batchEvent = new
{
    type = "messages",
    payload = new
    {
        topic = "orders",
        partition = 0,
        messages = new[]
        {
            new { offset = 42, key = "k1", value = "v1" },
            new { offset = 43, key = "k2", value = "v2" }
        }
    }
};
```

## Step 4: Produce via WebSocket

Send messages without a separate HTTP call:

```csharp
// Produce request
var produceRequest = new
{
    type = "produce",
    payload = new
    {
        topic = "orders",
        partition = 0,
        key = "order-1",
        value = "{\"id\":1,\"item\":\"widget\"}"
    },
    correlation_id = "prod-1"
};

// Response
var produceResponse = new
{
    type = "produced",
    payload = new
    {
        topic = "orders",
        partition = 0,
        offset = 43
    },
    correlation_id = "prod-1"
};
```

## Step 5: Admin Operations via WebSocket

Query topic and cluster information:

```csharp
// List topics
var listTopics = new { type = "list_topics", correlation_id = "admin-1" };

// Response
var topicsResponse = new
{
    type = "topics",
    payload = new
    {
        topics = new[] { "orders", "inventory", "payments" }
    },
    correlation_id = "admin-1"
};

// Describe topic
var describeTopic = new
{
    type = "describe_topic",
    payload = new { topic = "orders" },
    correlation_id = "admin-2"
};

// Get cluster info
var clusterInfo = new { type = "get_cluster_info", correlation_id = "admin-3" };
```

## Step 6: Handle Heartbeat

Keep the connection alive with heartbeats:

```csharp
// Client sends heartbeat
var heartbeat = new { type = "heartbeat" };

// Server responds
var heartbeatAck = new { type = "heartbeat_ack" };
```

## Step 7: Unsubscribe

Stop receiving messages from a subscription:

```csharp
var unsubscribe = new
{
    type = "unsubscribe",
    payload = new
    {
        subscription_id = "abc123"
    },
    correlation_id = "unsub-1"
};

var unsubscribeResponse = new
{
    type = "unsubscribed",
    payload = new
    {
        subscription_id = "abc123"
    },
    correlation_id = "unsub-1"
};
```

## Complete Solution

See `completed/Program.cs` for simulation of WebSocket message flow.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 20.3 - WebSocket Streaming ===

Surgewave broker started on port 51234

--- WebSocket Connection ---

Connecting to: ws://gateway:8080/ws
Session created with ID: session-abc123

--- Subscription Flow ---

Sending subscribe request:
{
  "type": "subscribe",
  "payload": { "topic": "orders", "partition": 0, "offset": -1 },
  "correlation_id": "sub-1"
}

Received subscription confirmation:
{
  "type": "subscribed",
  "payload": { "topic": "orders", "partition": 0, "subscription_id": "sub-xyz" },
  "correlation_id": "sub-1"
}

--- Producing via WebSocket ---

Sending produce request:
{
  "type": "produce",
  "payload": { "topic": "orders", "key": "order-1", "value": "{\"id\":1}" },
  "correlation_id": "prod-1"
}

Received produce confirmation:
{
  "type": "produced",
  "payload": { "topic": "orders", "partition": 0, "offset": 0 },
  "correlation_id": "prod-1"
}

--- Receiving Streamed Messages ---

Received message push:
{
  "type": "message",
  "payload": { "topic": "orders", "partition": 0, "offset": 0, "key": "order-1", "value": "{\"id\":1}" }
}

--- Admin Operations ---

List topics response:
{
  "type": "topics",
  "payload": { "topics": ["orders"] }
}

--- Heartbeat ---

Sent: { "type": "heartbeat" }
Received: { "type": "heartbeat_ack" }

--- Key Takeaways ---
1. WebSocket provides real-time bidirectional streaming
2. Subscribe once, receive messages as they arrive
3. Produce and consume over same connection
4. Heartbeat keeps connection alive
5. Use for dashboards and real-time applications

Lesson complete!
```

## Knowledge Check

1. What's the advantage of WebSocket over REST polling?
2. How does a client subscribe to a topic?
3. What triggers message delivery to WebSocket clients?
4. Why is heartbeat important?
5. What happens when a subscription buffer overflows?

---

**Next:** [Lesson 20.4: gRPC Clients](../lesson-4/README.md)
