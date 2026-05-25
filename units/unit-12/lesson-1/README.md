# Lesson 12.1: Log vs Queue — Two Worlds, One System

Traditional brokers force you to pick a camp: either a **log** (Kafka-style) or a **queue**
(RabbitMQ-style). Understanding the trade-offs of each makes it clear why Surgewave's hybrid model
is worth the extra concepts.

## The Log Model (Kafka-style)

In a log-based broker, every message is stored as an immutable, offset-addressed record inside a
partitioned log. Consumers never "consume" messages in a destructive sense — they advance a read
pointer called an **offset**. The log itself is unchanged.

Key properties:

- **Replayable** — reset the offset to re-read any part of history
- **Multi-consumer** — many consumer groups can read the same partition independently
- **Durable** — messages survive until the retention period expires, regardless of delivery status
- **No ack** — consumers report "processed up to offset N" rather than "processed message X"

What the log model lacks:

- No built-in requeue — if your consumer crashes mid-message, you must reset the offset manually
- No visibility timeout — another consumer reads from where the cursor is, not from where failures are
- No DLQ — routing poisoned messages to a dead-letter destination requires external logic

### Log Consumer in Surgewave (.NET)

```csharp
// Standard Kafka-style offset-based consumer — no ack required
var consumer = new SurgewaveConsumer<string, string>(new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "analytics-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
});

consumer.Subscribe("orders");

while (true)
{
    var result = consumer.Consume(CancellationToken.None);
    Console.WriteLine($"Offset {result.Offset}: {result.Message.Value}");
    // No ack call — offset advances automatically
}
```

## The Queue Model (RabbitMQ-style)

In a queue-based broker, messages are delivered to exactly one consumer at a time and are hidden
from others while in delivery. The consumer must explicitly **acknowledge** each message.

Key properties:

- **At-least-once delivery** — unacknowledged messages are re-delivered automatically
- **Visibility timeout** — messages are hidden while in-flight; expired timeouts trigger re-delivery
- **Dead-letter routing** — messages that fail too many times are routed to a DLQ automatically
- **Requeue** — `Basic.Nack(requeue=true)` places a message back into the delivery queue

What the queue model lacks:

- **No replay** — acknowledged messages are gone; you cannot go back and re-read them
- **No multi-consumer replay** — you cannot attach a second "analytics" reader to the same queue
- **Storage pressure** — messages must be deleted as they are consumed to free space

### Queue Consumer (RabbitMQ .NET Client)

```csharp
// RabbitMQ-style explicit ack — message is hidden until ack or timeout
var channel = await connection.CreateChannelAsync();

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        ProcessOrder(ea.Body.ToArray());
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);  // Done — remove from queue
    }
    catch (Exception)
    {
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);  // Re-deliver
    }
};

await channel.BasicConsumeAsync("orders", autoAck: false, consumer: consumer);
```

## Surgewave QueueView: the Hybrid Approach

QueueView is a thin stateful overlay that sits in front of a Surgewave partition log. The log itself
is never modified — messages are appended once and remain there until retention removes them.

```
Producer  ---> [ Surgewave Partition Log ] ---> Kafka-style Consumer Group (offset-based)
                         |
                         |  (same immutable log, untouched by QueueView)
                         v
                  [ QueueView Overlay ]
                         |
                         v
               AMQP Client (RabbitMQ SDK, Azure AMQP, etc.)
```

- The **log layer** handles retention, replication, and replay — unchanged.
- The **QueueView layer** tracks which offsets are in-flight, when they expire, how many times
  they have been delivered, and where to route them when they exceed the delivery limit.

The same topic can be read by a Kafka consumer (by offset) and an AMQP consumer (via QueueView)
**at exactly the same time**. Neither path interferes with the other.

## Comparison Table

| Aspect | Log (Kafka-style) | Queue (QueueView-style) |
|--------|-------------------|------------------------|
| Message storage | Immutable, offset-addressed | Immutable; delivery state tracked separately |
| Consume style | Advance offset pointer | Deliver, hide, ack or re-deliver |
| Failure handling | Manually reset offset | Visibility timeout expiry, automatic re-delivery |
| Dead-letter routing | Not built-in | Automatic DLQ after `MaxDeliveryCount` |
| Requeue | Not supported | Yes — `Nack(requeue: true)` |
| Log replay | Always available | Always available (QueueView state is separate) |
| Multi-consumer | Yes (independent offsets) | One active delivery path per QueueView topic |

> **Key insight:** You do not choose between log and queue in Surgewave — you get both.
> Produce once, replay with Kafka tooling, and deliver to AMQP clients — all from the same topic.

---

[Back to Unit 12](../README.md)
