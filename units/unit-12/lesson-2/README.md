# Lesson 12.2: Configuring QueueView

QueueView is disabled by default. Every setting lives under the `Surgewave:QueueView` configuration
section and follows Surgewave's standard feature-toggle pattern.

## QueueViewConfig Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `false` | Enable QueueView semantics globally. Topics must also be enrolled. |
| `VisibilityTimeout` | `TimeSpan` | `00:00:30` | How long a delivered message is hidden before it becomes eligible for re-delivery. |
| `MaxDeliveryCount` | `int` | `5` | Maximum delivery attempts before routing to the dead-letter topic. |
| `DlqTopicSuffix` | `string` | `".dlq"` | Suffix appended to the source topic name to derive the DLQ topic name. Set to empty string to disable DLQ routing. |
| `CleanupInterval` | `TimeSpan` | `00:00:10` | How often the background timer checks for expired visibility timeouts. |
| `MaxInFlightPerConsumer` | `int` | `1000` | Maximum in-flight (delivered-but-not-acknowledged) messages per consumer. Soft cap. |

## Enabling QueueView

### appsettings.json

```json
{
  "Surgewave": {
    "QueueView": {
      "Enabled": true,
      "VisibilityTimeout": "00:00:30",
      "MaxDeliveryCount": 5,
      "DlqTopicSuffix": ".dlq",
      "CleanupInterval": "00:00:10",
      "MaxInFlightPerConsumer": 1000
    }
  }
}
```

### Command-line flags

```bash
dotnet run --project src/Kuestenlogik.Surgewave.Broker -- \
  --Surgewave:QueueView:Enabled=true \
  --Surgewave:QueueView:VisibilityTimeout=00:00:30 \
  --Surgewave:QueueView:MaxDeliveryCount=5
```

### Environment variables (Docker)

```bash
docker run -d \
  --name surgewave \
  -p 9092:9092 \
  -p 5672:5672 \
  -e Surgewave__QueueView__Enabled=true \
  -e Surgewave__QueueView__VisibilityTimeout=00:01:00 \
  -e Surgewave__QueueView__MaxDeliveryCount=3 \
  -e Surgewave__QueueView__DlqTopicSuffix=.dlq \
  surgewave:latest
```

> **Note:** Environment variables use double underscores (`__`) as the hierarchy separator.

## Topic Enrollment

Enabling QueueView in configuration is a necessary but not sufficient condition. Each topic must
also be **enrolled** in a QueueView before queue semantics apply.

Enrollment happens automatically in two situations:

1. **AMQP adapter** — when an AMQP client calls `Basic.Consume` on a queue, the adapter calls
   `QueueViewManager.GetOrCreate(topic, log)` to enrol the topic.
2. **Programmatic** — call `IQueueViewManager.GetOrCreate` directly in your service code.

Topics that are not enrolled are served by the standard log-based offset path, even when QueueView
is enabled globally.

## DLQ Configuration

The dead-letter topic name is derived automatically:

```
{sourceTopic}{DlqTopicSuffix}
```

For example, with the default suffix `.dlq`:

| Source Topic | DLQ Topic |
|-------------|-----------|
| `orders` | `orders.dlq` |
| `payments` | `payments.dlq` |
| `tenant/ns/events` | `tenant/ns/events.dlq` |

QueueView creates the DLQ topic on demand with:
- 1 partition, replication factor 1
- `cleanup.policy=delete`, 7-day retention

To disable DLQ routing entirely and drop undeliverable messages, set `DlqTopicSuffix` to an empty
string:

```json
{
  "Surgewave": {
    "QueueView": {
      "DlqTopicSuffix": ""
    }
  }
}
```

## REST API Endpoints

QueueView exposes three management endpoints under `/api/queue`.

### List enrolled topics

```bash
GET /api/queue/topics
```

Response:

```json
["orders", "payments", "shipments"]
```

### Get in-flight count for a topic

```bash
GET /api/queue/{topic}/status
```

Response:

```json
{
  "topic": "orders",
  "inFlightCount": 42
}
```

Returns `404 Not Found` when no QueueView exists for the topic.

### Purge in-flight state

```bash
POST /api/queue/{topic}/purge
```

Clears all in-flight message tracking for a topic. The underlying log is untouched — no messages
are lost. Consumers that reconnect after a purge receive messages from their last committed offset.

Response:

```json
{
  "topic": "orders",
  "message": "In-flight messages cleared. QueueView will be recreated on next receive call."
}
```

### Using curl

```bash
# List all topics with an active QueueView
curl http://localhost:5050/api/queue/topics

# Check in-flight count
curl http://localhost:5050/api/queue/orders/status

# Purge stuck in-flight state (safe — log is not modified)
curl -X POST http://localhost:5050/api/queue/orders/purge
```

## Production-Ready Configuration

For production workloads, consider tightening the timeout and delivery count:

```json
{
  "Surgewave": {
    "QueueView": {
      "Enabled": true,
      "VisibilityTimeout": "00:01:00",
      "MaxDeliveryCount": 3,
      "DlqTopicSuffix": ".dlq",
      "CleanupInterval": "00:00:05",
      "MaxInFlightPerConsumer": 500
    },
    "Amqp": {
      "Enabled": true,
      "Port": 5672,
      "MaxConnections": 200,
      "MaxFrameSize": 131072,
      "MaxChannels": 2047,
      "HeartbeatInterval": 60
    }
  },
  "Logging": {
    "LogLevel": {
      "Kuestenlogik.Surgewave.Broker.Queue": "Information",
      "Kuestenlogik.Surgewave.Protocol.Amqp": "Information"
    }
  }
}
```

---

[Back to Unit 12](../README.md)
