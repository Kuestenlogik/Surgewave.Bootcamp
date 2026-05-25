# Lesson 20.2: Instructions

Follow these step-by-step instructions to use REST API for producing and consuming.

## Step 1: Start the Broker and Create HttpClient

First, start the broker and set up an HTTP client for REST calls:

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

// In a real scenario, you'd point to the Gateway URL
// For this lesson, we simulate REST API behavior
var gatewayUrl = "http://localhost:8080";
```

## Step 2: Produce a Message via REST

Produce a message using the REST API endpoint:

```csharp
// REST API produce endpoint format:
// POST /v3/topics/{topic}/records
// POST /v3/clusters/{cluster_id}/topics/{topic}/records

var produceRequest = new
{
    record = new
    {
        key = Convert.ToBase64String(Encoding.UTF8.GetBytes("order-1")),
        value = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"id\":1,\"item\":\"widget\"}"))
    },
    partition = 0,
    acks_required = true
};

// Actual HTTP call would be:
// var response = await httpClient.PostAsJsonAsync(
//     $"{gatewayUrl}/v3/topics/orders/records",
//     produceRequest);
```

## Step 3: Fetch Messages via REST

Fetch messages from a topic/partition:

```csharp
// REST API fetch endpoint:
// POST /v3/consumers/fetch
// POST /v3/clusters/{cluster_id}/consumers/fetch

var fetchRequest = new
{
    partitions = new[]
    {
        new
        {
            topic = "orders",
            partition = 0,
            offset = 0L,
            max_bytes = 1048576
        }
    },
    max_wait_ms = 5000
};

// Actual HTTP call would be:
// var response = await httpClient.PostAsJsonAsync(
//     $"{gatewayUrl}/v3/consumers/fetch",
//     fetchRequest);
```

## Step 4: Commit Offsets via REST

Commit consumer group offsets:

```csharp
// REST API commit endpoint:
// POST /v3/consumer-groups/{group}/offsets
// POST /v3/clusters/{cluster_id}/consumer-groups/{group}/offsets

var commitRequest = new
{
    offsets = new[]
    {
        new
        {
            topic = "orders",
            partition = 0,
            offset = 1L,
            metadata = "processed"
        }
    }
};

// Actual HTTP call would be:
// var response = await httpClient.PostAsJsonAsync(
//     $"{gatewayUrl}/v3/consumer-groups/my-consumer/offsets",
//     commitRequest);
```

## Step 5: Fetch Committed Offsets

Retrieve committed offsets for a consumer group:

```csharp
// REST API fetch offsets endpoint:
// GET /v3/consumer-groups/{group}/offsets?partitions[topic]=orders&partitions[partition]=0

// Actual HTTP call would be:
// var response = await httpClient.GetAsync(
//     $"{gatewayUrl}/v3/consumer-groups/my-consumer/offsets");
```

## Step 6: Handle List Offsets

Get earliest/latest offsets for partitions:

```csharp
// REST API list offsets endpoint:
// POST /v3/topics/-/partitions/-/offsets

var listOffsetsRequest = new
{
    partitions = new[]
    {
        new
        {
            topic = "orders",
            partition = 0,
            timestamp = -1L  // -1 = latest, -2 = earliest
        }
    }
};
```

## Complete Solution

See `completed/Program.cs` for simulation of REST API calls using Surgewave native client.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 20.2 - REST Produce/Consume ===

Surgewave broker started on port 51234

--- Simulating REST API Produce ---

POST /v3/topics/orders/records
Request:
{
  "record": {
    "key": "b3JkZXItMQ==",
    "value": "eyJpZCI6MSwiaXRlbSI6IndpZGdldCJ9"
  },
  "partition": 0
}

Response:
{
  "topic": "orders",
  "partition": 0,
  "offset": 0,
  "status": { "error_code": "NONE" }
}

Produced 5 messages

--- Simulating REST API Fetch ---

POST /v3/consumers/fetch
Request:
{
  "partitions": [
    { "topic": "orders", "partition": 0, "offset": 0, "max_bytes": 1048576 }
  ],
  "max_wait_ms": 5000
}

Response:
{
  "results": [
    {
      "topic": "orders",
      "partition": 0,
      "records": [
        { "offset": 0, "key": "order-1", "value": {"id":1} },
        { "offset": 1, "key": "order-2", "value": {"id":2} },
        { "offset": 2, "key": "order-3", "value": {"id":3} },
        { "offset": 3, "key": "order-4", "value": {"id":4} },
        { "offset": 4, "key": "order-5", "value": {"id":5} }
      ],
      "high_watermark": 5
    }
  ]
}

Fetched 5 messages

--- Simulating REST API Offset Commit ---

POST /v3/consumer-groups/my-consumer/offsets
Request:
{
  "offsets": [
    { "topic": "orders", "partition": 0, "offset": 5 }
  ]
}

Response:
{
  "results": [
    { "topic": "orders", "partition": 0, "status": { "error_code": "NONE" } }
  ]
}

Committed offset 5

--- Key Takeaways ---
1. REST API uses JSON for request/response
2. Binary data (keys/values) are Base64 encoded
3. Fetch is pull-based (polling model)
4. Offsets must be explicitly committed
5. Error codes indicate success or failure type

Lesson complete!
```

## Knowledge Check

1. What HTTP method is used to produce messages?
2. How are binary keys/values encoded in JSON?
3. What's the difference between fetch and consume?
4. What does a timestamp of -1 mean in ListOffsets?
5. Why must offsets be committed explicitly?

---

**Next:** [Lesson 20.3: WebSocket Streaming](../lesson-3/README.md)
