# Lesson 1.4: Partitioning

> **Difficulty:** Beginner | **Duration:** 30 min | **Prerequisites:** Lesson 1.3

## Overview

In this lesson, you'll learn how partitioning enables parallel processing in Surgewave. Partitions are the fundamental unit of parallelism - they allow multiple consumers to process messages concurrently while maintaining order within each partition.

## Learning Objectives

By the end of this lesson, you will:

1. Create topics with multiple partitions
2. Understand how keys determine partition assignment
3. Produce to specific partitions
4. Consume from multiple partitions in parallel

## Key Concepts

### What are Partitions?

A partition is an ordered, immutable sequence of messages:

```
Topic: orders (3 partitions)

Partition 0: [msg0, msg3, msg6, msg9, ...]
Partition 1: [msg1, msg4, msg7, msg10, ...]
Partition 2: [msg2, msg5, msg8, msg11, ...]
```

### Key-Based Partitioning

By default, Surgewave uses the message key to determine the partition:

```csharp
// Messages with the same key go to the same partition
await producer.ProduceAsync("orders", "customer-123", order1);  // → Partition X
await producer.ProduceAsync("orders", "customer-123", order2);  // → Partition X (same!)
await producer.ProduceAsync("orders", "customer-456", order3);  // → Partition Y
```

This ensures **ordering** for messages with the same key.

### Creating Multi-Partition Topics

```csharp
// Using the native client
await using var client = new SurgewaveNativeClient("localhost", runtime.Port);
await client.ConnectAsync();

// Create a topic with 3 partitions
await client.Topics.CreateAsync("orders", partitions: 3);
```

### Producing to Specific Partitions

```csharp
// Let Surgewave choose (based on key hash)
await producer.ProduceAsync("orders", key, value);

// Or specify explicitly
await producer.ProduceAsync("orders", partition: 1, key, value);
```

### Partition Assignment in Consumer Groups

When using consumer groups, partitions are distributed:

```
Topic: orders (3 partitions)
Group: order-processors (2 consumers)

Consumer A: Partition 0, Partition 2
Consumer B: Partition 1
```

## Prerequisites

- Completed Lesson 1.4 (Consumer Groups)

## Getting Started

1. Navigate to the `start` folder
2. Open `Program.cs` and follow the TODO comments
3. Reference `INSTRUCTIONS.md` for detailed guidance

## Verification

Run the completed lesson:

```bash
cd completed
dotnet run
```

Expected output:
```
=== Surgewave Bootcamp: Lesson 1.5 - Partitioning ===

Starting embedded Surgewave broker...
Broker started on localhost:9092

Creating topic 'user-events' with 3 partitions...
Topic created!

Producing user events...
  Event for user-1 -> Partition ?
  Event for user-2 -> Partition ?
  Event for user-3 -> Partition ?
  Event for user-1 -> Partition ? (same as first user-1!)
  ...

Listing topic partitions...
  Topic: user-events
  Partitions: 3

Consuming from all partitions...
  [P0] user-1: login
  [P1] user-2: login
  [P2] user-3: login
  ...

Message distribution by partition:
  Partition 0: 4 messages
  Partition 1: 3 messages
  Partition 2: 3 messages

Shutting down...
Goodbye!
```

## Next Steps

Congratulations! You've completed Unit 1 of the Surgewave Bootcamp. You now understand the fundamentals of:
- Running an embedded broker
- Producing and consuming messages
- Consumer groups for coordinated consumption
- Partitions for parallel processing

Continue to [Unit 3: Testing Surgewave](../../unit-3/README.md) to learn about:
- Message headers
- Transactions
- Schema Registry
- And more!
