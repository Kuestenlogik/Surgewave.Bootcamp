# Unit 13 Project: Surgewave Web API

Build an ASP.NET Core Minimal API that exposes Surgewave functionality via REST endpoints.

## Scenario

You're building a REST API for a notification service. The API should:
1. Accept notification requests via HTTP POST
2. Produce events to Surgewave topics
3. Provide health check endpoints
4. Expose metrics and status information

## Requirements

### Functional Requirements

- [ ] POST `/api/notifications` - Send a notification (produces to Surgewave)
- [ ] GET `/api/notifications/{id}` - Get notification status
- [ ] GET `/api/topics` - List available topics
- [ ] GET `/api/health` - Health check endpoint
- [ ] GET `/api/metrics` - Basic metrics (message counts, latency)

### Technical Requirements

- [ ] Use ASP.NET Core Minimal API
- [ ] Register Surgewave client with dependency injection
- [ ] Implement proper request validation
- [ ] Use background service for consumer processing
- [ ] Add correlation ID middleware

## API Endpoints

```
POST /api/notifications
{
    "recipient": "user@example.com",
    "channel": "email",
    "subject": "Hello",
    "body": "World",
    "priority": "high"
}

Response: 202 Accepted
{
    "notificationId": "NOT-001",
    "status": "queued",
    "timestamp": "2025-01-25T10:30:00Z"
}

GET /api/notifications/NOT-001
Response: 200 OK
{
    "notificationId": "NOT-001",
    "status": "delivered",
    "deliveredAt": "2025-01-25T10:30:05Z"
}

GET /api/topics
Response: 200 OK
{
    "topics": [
        { "name": "notifications-email", "partitions": 3 },
        { "name": "notifications-sms", "partitions": 3 },
        { "name": "notifications-push", "partitions": 3 }
    ]
}

GET /api/health
Response: 200 OK
{
    "status": "healthy",
    "broker": "connected",
    "uptime": "00:15:32"
}

GET /api/metrics
Response: 200 OK
{
    "messagesProduced": 1523,
    "messagesConsumed": 1520,
    "avgLatencyMs": 12.5,
    "errorsCount": 3
}
```

## Expected Output

```
=== Surgewave Notification API ===

[Startup] Initializing Surgewave client...
[Startup] Creating topics...
[Startup] Starting background consumer...
[Startup] API ready on http://localhost:5000

[API] POST /api/notifications - Notification NOT-001 queued (email)
[Consumer] Processing NOT-001 for user@example.com
[Consumer] NOT-001 delivered successfully

[API] POST /api/notifications - Notification NOT-002 queued (sms)
[Consumer] Processing NOT-002 for +1234567890
[Consumer] NOT-002 delivered successfully

[API] GET /api/health - Status: healthy
[API] GET /api/metrics - Produced: 2, Consumed: 2, Errors: 0

Press Ctrl+C to shutdown...

[Shutdown] Stopping consumer...
[Shutdown] Flushing producer...
[Shutdown] API shutdown complete
```

## Getting Started

```bash
cd bootcamp/unit-13/project/start
dotnet run
```

Then test with:
```bash
curl -X POST http://localhost:5000/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"recipient":"test@example.com","channel":"email","subject":"Test","body":"Hello"}'
```

## Hints

1. Use `builder.Services.AddSingleton<SurgewaveClient>()` for DI
2. Use `app.MapPost("/api/notifications", ...)` for endpoints
3. Implement `BackgroundService` for the consumer
4. Use `IHostApplicationLifetime` for graceful shutdown
5. Add correlation ID via middleware: `app.Use(async (ctx, next) => ...)`

## Evaluation Criteria

| Criteria | Points |
|----------|--------|
| Notification endpoint works | 25 |
| Background consumer processes | 20 |
| Health check accurate | 15 |
| Metrics tracking | 15 |
| Proper DI and lifecycle | 15 |
| Correlation ID middleware | 10 |

## Solution

Compare your implementation with [completed/](completed/README.md) when done.
