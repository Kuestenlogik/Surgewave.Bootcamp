# Lesson 1.2: Consuming Messages

> **Difficulty:** Beginner | **Duration:** 30 min | **Prerequisites:** Lesson 1.1

## Overview

In this lesson, you'll learn how to consume messages from Surgewave topics using the `SurgewaveConsumer<TKey, TValue>` API. You'll understand manual assignment, offset tracking, and message deserialization.

## Learning Objectives

By the end of this lesson, you will:

1. Create a Surgewave consumer with configuration
2. Subscribe to topics and consume messages
3. Understand offset management
4. Handle message deserialization

## Key Concepts

### SurgewaveConsumer<TKey, TValue>

The `SurgewaveConsumer` is a strongly-typed consumer that handles deserialization:

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";
});
consumer.Subscribe("my-topic");
```

### Subscription Modes

Surgewave supports two ways to consume:

1. **Subscribe**: Automatically discover and consume from all partitions
   ```csharp
   consumer.Subscribe("topic1", "topic2");
   ```

2. **Assign**: Manually specify which partition(s) to consume
   ```csharp
   consumer.Assign("topic", partition: 0, offset: 0);
   ```

### ConsumeResult<TKey, TValue>

Each consume operation returns a result containing:
- `Topic` - The source topic
- `Partition` - The partition number
- `Offset` - The message offset
- `Timestamp` - When the message was produced
- `Key` - The deserialized key
- `Value` - The deserialized value
- `Headers` - Optional message headers

### AutoOffsetReset

When a consumer starts, it needs to know where to begin:
- `Earliest` - Start from the beginning (offset 0)
- `Latest` - Start from new messages only

## Prerequisites

- Completed Lesson 1.2 (Producing Messages)

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
=== Surgewave Bootcamp: Lesson 1.3 - Consuming Messages ===

Starting embedded Surgewave broker...
Broker started on localhost:9092

Producing messages first...
  Produced: Hello Surgewave!
  Produced: Surgewave is fast!
  Produced: And easy to use!

Creating consumer...
Consumer ready!

Consuming messages...
  Received [0]: Key='user-1', Value='Hello Surgewave!'
  Received [1]: Key='user-2', Value='Surgewave is fast!'
  Received [2]: Key='user-1', Value='And easy to use!'

Consumed 3 messages from 'greetings' topic

Shutting down...
Goodbye!
```

## Next Steps

Continue to [Lesson 1.4: Consumer Groups](../lesson-4/README.md) to learn about coordinated consumption with multiple consumers!
