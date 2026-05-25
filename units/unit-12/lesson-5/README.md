# Lesson 12.5: Priority Lanes

Not all messages are equal. An urgent payment failure alert should not wait behind a batch of
low-priority analytics events. Surgewave's priority lane system lets producers tag messages with a
priority level and routes them to dedicated partitions, ensuring high-priority messages are always
processed first.

## MessagePriority Enum

```csharp
public enum MessagePriority
{
    Low = 0,
    Normal = 1,
    High = 2
}
```

Three levels cover the common case: normal day-to-day traffic, high-urgency events that need
immediate attention, and low-priority background work that should yield to everything else.

## The surgewave-priority Header

Priority is communicated via a message header named `surgewave-priority`. Any producer — native Surgewave
client, AMQP client, REST API — can set this header.

### Native Surgewave client

```csharp
var message = new Message<string, string>
{
    Key = "order-456",
    Value = """{"type": "payment_failed", "amount": 250.00}""",
    Headers = new Headers
    {
        { "surgewave-priority", Encoding.UTF8.GetBytes("High") }
    }
};

await producer.ProduceAsync("payments", message);
```

### AMQP client (RabbitMQ SDK)

```csharp
var props = new BasicProperties
{
    Headers = new Dictionary<string, object?>
    {
        ["surgewave-priority"] = "High"
    }
};

await channel.BasicPublishAsync(
    exchange: "",
    routingKey: "payments",
    basicProperties: props,
    body: Encoding.UTF8.GetBytes("""{"type": "payment_failed"}"""));
```

### REST API

```bash
curl -X POST http://localhost:5050/api/topics/payments/messages \
  -H "Content-Type: application/json" \
  -H "surgewave-priority: High" \
  -d '{"key": "order-456", "value": "{\"type\": \"payment_failed\"}"}'
```

## PriorityPartitioner: Partition-Based Routing

Priority lanes are implemented by routing messages to different partitions based on their priority
level. `PriorityPartitioner` intercepts the partition assignment and maps each priority to a
dedicated partition range.

### Topic Layout

For a topic with 6 partitions and 3 priority levels:

| Partitions | Priority |
|-----------|----------|
| 0–1 | High |
| 2–3 | Normal |
| 4–5 | Low |

High-priority partitions are consumed first, ensuring they drain before Normal and Low partitions
are processed.

### Configuring the PriorityPartitioner

```json
{
  "Surgewave": {
    "Topics": {
      "payments": {
        "Partitions": 6,
        "Partitioner": "Kuestenlogik.Surgewave.Core.Priority.PriorityPartitioner",
        "PriorityPartitions": {
          "High": [0, 1],
          "Normal": [2, 3],
          "Low": [4, 5]
        }
      }
    }
  }
}
```

## PriorityConsumerConfig: Weighted Polling

On the consumer side, `PriorityConsumerConfig` controls how much time the consumer spends polling
each priority partition. Weighted polling ensures high-priority partitions are checked more often:

```csharp
var config = new PriorityConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "payment-processors",
    PriorityWeights = new Dictionary<MessagePriority, int>
    {
        [MessagePriority.High] = 10,    // Poll High partitions 10x more than Low
        [MessagePriority.Normal] = 3,
        [MessagePriority.Low] = 1
    }
};

var consumer = new PriorityConsumer<string, string>(config);
consumer.Subscribe("payments");
```

The consumer polls partitions in a weighted round-robin order. For the weights above, for every
10 polls from High partitions, it does 3 from Normal and 1 from Low.

## Complete Configuration Example

### Producer (appsettings.json)

```json
{
  "Surgewave": {
    "Producer": {
      "BootstrapServers": "localhost:9092",
      "Partitioner": "Kuestenlogik.Surgewave.Core.Priority.PriorityPartitioner"
    }
  }
}
```

### Producer code

```csharp
// High priority — route to partition 0 or 1
await producer.ProduceAsync("payments", new Message<string, string>
{
    Key = "order-789",
    Value = """{"type": "chargeback", "amount": 1200.00}""",
    Headers = new Headers { { "surgewave-priority", Encoding.UTF8.GetBytes("High") } }
});

// Normal priority — route to partition 2 or 3
await producer.ProduceAsync("payments", new Message<string, string>
{
    Key = "order-790",
    Value = """{"type": "purchase", "amount": 49.99}""",
    Headers = new Headers { { "surgewave-priority", Encoding.UTF8.GetBytes("Normal") } }
});

// Low priority — route to partition 4 or 5
await producer.ProduceAsync("payments", new Message<string, string>
{
    Key = "batch-001",
    Value = """{"type": "daily_summary"}""",
    Headers = new Headers { { "surgewave-priority", Encoding.UTF8.GetBytes("Low") } }
});
```

### Consumer code

```csharp
var consumer = new PriorityConsumer<string, string>(new PriorityConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "payment-processors",
    PriorityWeights = new Dictionary<MessagePriority, int>
    {
        [MessagePriority.High] = 10,
        [MessagePriority.Normal] = 3,
        [MessagePriority.Low] = 1
    }
});

consumer.Subscribe("payments");

while (true)
{
    var result = consumer.Consume(CancellationToken.None);

    // Priority is available via the header
    var priorityHeader = result.Message.Headers
        .FirstOrDefault(h => h.Key == "surgewave-priority");
    var priority = priorityHeader != null
        ? Enum.Parse<MessagePriority>(Encoding.UTF8.GetString(priorityHeader.GetValueBytes()))
        : MessagePriority.Normal;

    Console.WriteLine($"[{priority}] Processing: {result.Message.Value}");
    consumer.Commit(result);
}
```

## Priority Lanes via AMQP

AMQP clients can use priority lanes by setting the `surgewave-priority` message header:

```csharp
var props = new BasicProperties
{
    Headers = new Dictionary<string, object?>
    {
        ["surgewave-priority"] = "High"
    },
    ContentType = "application/json",
    DeliveryMode = DeliveryModes.Persistent
};

await channel.BasicPublishAsync(
    exchange: "",
    routingKey: "payments",
    basicProperties: props,
    body: Encoding.UTF8.GetBytes("""{"type": "fraud_alert"}"""));
```

Surgewave's AMQP adapter reads the `surgewave-priority` header and applies `PriorityPartitioner` routing
before appending to the log.

## When to Use Priority Lanes

| Use Case | Priority |
|----------|----------|
| Fraud alerts, payment failures, security events | High |
| Standard order processing, user actions | Normal |
| Batch jobs, analytics aggregation, reporting | Low |
| Background cleanup, audit log compaction | Low |

> **Tip:** Keep the number of priority levels small (2–3). More levels increase partition count
> and complexity without proportional benefit. Use High/Normal for most workloads; add Low only
> when you have a clear background-work use case.

---

[Back to Unit 12](../README.md)
