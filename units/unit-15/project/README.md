# Unit 20 Project: REST Gateway Bridge

Build a simple HTTP gateway that bridges REST API clients to Surgewave messaging.

## Scenario

You're building a lightweight REST API that allows web clients to produce and consume messages to/from Surgewave without using the native client libraries. This is useful for:
- Web browsers using fetch() API
- Legacy systems that only support HTTP
- Quick prototyping and debugging
- Cross-platform integration

Your REST gateway will:
1. Start an HTTP listener on a configurable port
2. Accept POST requests to produce messages to topics
3. Accept GET requests to consume messages from topics
4. Return JSON responses with message metadata

## Requirements

### Functional Requirements

- [ ] Start an embedded Surgewave broker and HTTP listener
- [ ] `POST /topics/{topic}` - Produce a message
- [ ] `GET /topics/{topic}?offset={offset}` - Consume messages
- [ ] `GET /topics` - List all available topics
- [ ] Return proper HTTP status codes and JSON responses

### Technical Requirements

- [ ] Use `HttpListener` for HTTP server
- [ ] Parse URL paths and query parameters
- [ ] Serialize/deserialize JSON using `System.Text.Json`
- [ ] Handle concurrent requests properly
- [ ] Implement graceful shutdown

## REST API Specification

### List Topics
```
GET /topics

Response 200:
{
  "topics": [
    { "name": "orders", "partitions": 3 },
    { "name": "events", "partitions": 1 }
  ]
}
```

### Produce Message
```
POST /topics/{topic}
Content-Type: application/json

{
  "key": "order-123",
  "value": "{ \"amount\": 99.95 }"
}

Response 201:
{
  "topic": "orders",
  "partition": 0,
  "offset": 42,
  "timestamp": "2024-01-25T10:30:00Z"
}
```

### Consume Messages
```
GET /topics/{topic}?partition=0&offset=0&limit=10

Response 200:
{
  "messages": [
    {
      "offset": 0,
      "key": "order-123",
      "value": "{ \"amount\": 99.95 }",
      "timestamp": "2024-01-25T10:30:00Z"
    }
  ],
  "nextOffset": 1
}
```

## Expected Output

```
=== REST Gateway Bridge ===

[Surgewave] Starting embedded broker...
[Surgewave] Broker started on port 9092

[Gateway] Starting HTTP listener on http://localhost:8080/
[Gateway] Ready to accept requests

[Request] POST /topics/orders
[Surgewave] Produced message to orders:0 at offset 0
[Response] 201 Created

[Request] GET /topics
[Response] 200 OK - 2 topics

[Request] GET /topics/orders?offset=0
[Surgewave] Consumed 1 message(s) from orders
[Response] 200 OK

[Gateway] Shutting down...
=== Gateway Complete ===
```

## Getting Started

```bash
cd bootcamp/unit-20/project/start
dotnet run
```

Then test with curl:
```bash
# List topics
curl http://localhost:8080/topics

# Produce a message
curl -X POST http://localhost:8080/topics/orders \
  -H "Content-Type: application/json" \
  -d '{"key":"order-1","value":"test message"}'

# Consume messages
curl http://localhost:8080/topics/orders?offset=0
```

## Hints

1. Use `HttpListener.GetContextAsync()` for async request handling
2. Parse the URL path using `context.Request.Url.AbsolutePath`
3. Read POST body with `StreamReader` on `context.Request.InputStream`
4. Set `context.Response.ContentType = "application/json"`
5. Remember to call `context.Response.Close()` after writing

## Implementation Structure

```csharp
// Main request loop
while (!cancellationToken.IsCancellationRequested)
{
    var context = await listener.GetContextAsync();
    _ = HandleRequestAsync(context); // Fire and forget
}

// Route requests
async Task HandleRequestAsync(HttpListenerContext context)
{
    var path = context.Request.Url?.AbsolutePath ?? "/";
    var method = context.Request.HttpMethod;

    if (path == "/topics" && method == "GET")
        await HandleListTopicsAsync(context);
    else if (path.StartsWith("/topics/") && method == "POST")
        await HandleProduceAsync(context);
    else if (path.StartsWith("/topics/") && method == "GET")
        await HandleConsumeAsync(context);
    else
        await SendNotFoundAsync(context);
}
```

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| HTTP listener starts correctly | 15 |
| List topics endpoint works | 15 |
| Produce endpoint stores messages | 25 |
| Consume endpoint returns messages | 25 |
| Proper JSON responses | 10 |
| Error handling | 10 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
