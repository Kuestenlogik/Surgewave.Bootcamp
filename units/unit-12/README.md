# Unit 12: Queue Semantics & AMQP

Surgewave is not just a log. With QueueView, every Surgewave topic can simultaneously serve Kafka-style
consumers (offset-based, replayable) and RabbitMQ-style consumers (ack/nack, visibility timeout,
dead-letter routing) — without duplicating data or running two brokers.

This unit covers the QueueView architecture, its configuration, the AMQP 0.9.1 protocol adapter,
priority message lanes, and a step-by-step guide for migrating existing RabbitMQ workloads to Surgewave.

## Learning Objectives

By the end of this unit, you will be able to:

- Explain the fundamental difference between a log model and a queue model
- Describe how Surgewave's QueueView overlays queue semantics on top of an immutable log
- Configure QueueView via `appsettings.json` and command-line flags
- Enable the AMQP adapter and connect any RabbitMQ-compatible client to Surgewave
- Understand visibility timeout, delivery count, and dead-letter routing
- Use priority lanes to route high-importance messages ahead of the queue
- Migrate a RabbitMQ application to Surgewave with minimal code changes

## Lessons

| Lesson | Topic | Description |
|--------|-------|-------------|
| 35.1 | Log vs Queue — Two Worlds, One System | Log and queue models compared; Surgewave's hybrid approach |
| 35.2 | Configuring QueueView | QueueViewConfig properties, appsettings.json, REST API |
| 35.3 | AMQP Protocol Adapter | AMQP 0.9.1 support, exchange/queue mapping, enabling AMQP |
| 35.4 | Visibility Timeout & Delivery Guarantees | In-flight lifecycle, DLQ routing, ack/nack/reject |
| 35.5 | Priority Lanes | MessagePriority, surgewave-priority header, PriorityPartitioner |
| 35.6 | Migrating from RabbitMQ | Step-by-step migration, feature mapping, before/after code |

## Prerequisites

- Completed Unit 1: Surgewave Fundamentals
- Completed Unit 3: Topics, Partitions & Retention
- Surgewave Broker running (QueueView and AMQP features are opt-in via configuration)

---

## Lesson 12.1: Log vs Queue — Two Worlds, One System

Traditional brokers force you to pick a camp: either a **log** (Kafka-style) or a **queue**
(RabbitMQ-style). Understanding the trade-offs of each camp makes it clear why Surgewave's hybrid model
is worth the extra concepts.

### The Log Model (Kafka-style)

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

### The Queue Model (RabbitMQ-style)

In a queue-based broker, messages are delivered to exactly one consumer at a time and are hidden
from others while in delivery. The consumer must explicitly acknowledge each message.

Key properties:
- **At-least-once delivery** — unacknowledged messages are re-delivered automatically
- **Visibility timeout** — messages are hidden while in-flight; expired timeouts trigger re-delivery
- **Dead-letter routing** — messages that fail too many times are routed to a DLQ automatically
- **Requeue** — `Basic.Nack(requeue=true)` places a message back at the head of the queue

What the queue model lacks:
- **No replay** — acknowledged messages are gone; you cannot go back and re-read them
- **No multi-consumer replay** — you cannot attach a second "analytics" reader to the same queue
- **Storage pressure** — messages must be deleted as they are consumed to free space

### Surgewave QueueView: the Hybrid Approach

QueueView is a thin stateful overlay that sits in front of a Surgewave partition log. The log itself
is never modified — messages are appended once and remain there until retention removes them.
QueueView adds a delivery-tracking layer on top:

```
Producer  ---> [ Surgewave Partition Log ] ---> Kafka-style Consumer Group
                         |
                         |  (immutable log, never touched by QueueView)
                         v
                  [ QueueView Overlay ]
                         |
                         v
               AMQP Client / RabbitMQ SDK
```

- The **log layer** handles retention, replication, and replay — unchanged.
- The **QueueView layer** tracks which offsets are in-flight, when they expire, how many times
  they have been delivered, and where to route them if they fail too many times.

The same topic can be read by a Kafka consumer (by offset) and an AMQP consumer (via QueueView)
at exactly the same time. Neither path interferes with the other.

### Comparison Table

| Aspect | Log (Kafka-style) | Queue (QueueView-style) |
|--------|-------------------|------------------------|
| Message storage | Immutable, offset-addressed | Immutable, delivery state tracked separately |
| Consume style | Advance offset pointer | Deliver, hide, ack or re-deliver |
| Failure handling | Manually reset offset | Visibility timeout expiry, automatic re-delivery |
| Dead-letter routing | Not built-in | Automatic DLQ after `MaxDeliveryCount` |
| Requeue | Not supported | Yes — `Nack(requeue: true)` |
| Log replay | Always available | Always available (QueueView state is separate) |
| Multi-consumer | Yes (independent offsets) | No (one active delivery path per topic) |

---

[Back to Unit 12](README.md)
