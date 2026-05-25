# Knowledge Assessment: Lesson 20.1 - Gateway Architecture

## Multiple Choice

### 1. Which protocol provides the lowest latency for microservice-to-microservice communication through the Gateway?
- [ ] A) REST (HTTP/JSON)
- [ ] B) gRPC (HTTP/2 + Protobuf)
- [ ] C) WebSocket
- [ ] D) All have equal latency

<details>
<summary>Answer</summary>
**B)** gRPC provides the lowest latency due to binary Protobuf serialization, HTTP/2 multiplexing, and persistent connections. REST has JSON parsing overhead, and WebSocket has connection establishment overhead.
</details>

### 2. What is the default cluster routing behavior when cluster_id is omitted from a Gateway URL?
- [ ] A) The request fails with an error
- [ ] B) The request is load-balanced across all clusters
- [ ] C) The request is routed to the configured default cluster
- [ ] D) The request is broadcast to all clusters

<details>
<summary>Answer</summary>
**C)** When cluster_id is omitted, the request is routed to the configured DefaultCluster. For example, POST /v3/topics/orders/records uses the default cluster.
</details>

### 3. Which Gateway service handles consumer group operations?
- [ ] A) ConsumerService
- [ ] B) TopicService
- [ ] C) ConsumerGroupService
- [ ] D) ClusterService

<details>
<summary>Answer</summary>
**C)** ConsumerGroupService handles group management operations including Describe, List, and Delete consumer groups. ConsumerService handles message consumption operations.
</details>

### 4. What is the primary disadvantage of REST API compared to gRPC?
- [ ] A) No browser support
- [ ] B) Requires client code generation
- [ ] C) Higher latency and larger payloads
- [ ] D) Cannot handle authentication

<details>
<summary>Answer</summary>
**C)** REST API has higher latency due to JSON parsing and larger payloads due to text-based formatting. REST actually has advantages in browser support and not requiring code generation.
</details>

### 5. In the Confluent v3 API compatible routes, which endpoint is used to produce a message?
- [ ] A) PUT /v3/topics/{name}/records
- [ ] B) POST /v3/topics/{name}/records
- [ ] C) POST /v3/producers/{name}/send
- [ ] D) POST /v3/messages/{topic}

<details>
<summary>Answer</summary>
**B)** POST /v3/topics/{name}/records is the endpoint for producing records to a topic, following the Confluent REST Proxy v3 API specification.
</details>

## True/False

### 6. The Gateway performs JSON to Protobuf transcoding automatically for REST endpoints.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The Gateway uses gRPC JSON transcoding to automatically convert between JSON (REST) and Protobuf (gRPC) formats, allowing REST clients to access gRPC services.
</details>

## Code Challenge

### 7. What's wrong with this Gateway configuration?

```json
{
  "Gateway": {
    "Clusters": {
      "production": {
        "BootstrapServers": "surgewave-prod:9092"
      },
      "staging": {
        "BootstrapServers": "surgewave-staging:9092"
      }
    },
    "ConnectionTimeoutMs": 30000,
    "RequestTimeoutMs": 30000
  }
}
```

<details>
<summary>Answer</summary>
The configuration is missing the DefaultCluster setting. Without it, requests that don't specify a cluster_id will fail or behave unpredictably:

```json
{
  "Gateway": {
    "DefaultCluster": "production",  // Missing! Required for routing
    "Clusters": {
      "production": {
        "BootstrapServers": "surgewave-prod:9092"
      },
      "staging": {
        "BootstrapServers": "surgewave-staging:9092"
      }
    },
    "ConnectionTimeoutMs": 30000,
    "RequestTimeoutMs": 30000
  }
}
```

The DefaultCluster specifies which cluster receives requests when the URL doesn't include an explicit cluster_id.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 3-4/7: Review the lesson
- <3/7: Re-do the lesson
