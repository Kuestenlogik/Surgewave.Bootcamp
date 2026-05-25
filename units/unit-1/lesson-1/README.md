# Lesson 1.1: Producing Messages

> **Difficulty:** Beginner | **Duration:** 30 min | **Prerequisites:** Unit 0 completion

## Overview

In this lesson, you'll learn how to produce messages to Surgewave topics using the strongly-typed `SurgewaveProducer<TKey, TValue>` API. You'll understand how Surgewave handles serialization and how to send different types of data.

## Learning Objectives

By the end of this lesson, you will:

1. Create a Surgewave producer with configuration
2. Produce simple string messages
3. Work with typed messages (JSON serialization)
4. Understand delivery confirmation

## Key Concepts

### SurgewaveProducer<TKey, TValue>

The `SurgewaveProducer` is a strongly-typed producer that handles serialization for you:

```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";
});
```

### Serialization

Surgewave automatically selects serializers based on your types:

| Type | Default Serializer |
|------|-------------------|
| `string` | UTF-8 string serializer |
| `byte[]` | Pass-through (no transformation) |
| `int`, `long` | Binary serializers |
| `Guid` | Binary GUID serializer |
| Complex types | JSON serializer |

You can override serializers:

```csharp
options.ValueSerializer = Serializers.Json<MyClass>();
```

### ProduceResult

Each produce operation returns a `ProduceResult` containing:
- `Topic` - The topic the message was written to
- `Partition` - The partition number
- `Offset` - The offset where the message was stored
- `Timestamp` - When the message was produced

## Prerequisites

- Completed Lesson 1.1 (Hello Surgewave)
- Understanding of async/await in C#

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
=== Surgewave Bootcamp: Lesson 1.2 - Producing Messages ===

Starting embedded Surgewave broker...
Broker started on localhost:9092

Creating producer...
Producer ready!

Producing messages...
  Message 1: key='user-1', value='Hello Surgewave!' -> Offset 0
  Message 2: key='user-2', value='Surgewave is fast!' -> Offset 1
  Message 3: key='user-1', value='And easy to use!' -> Offset 2

Produced 3 messages to 'greetings' topic

Shutting down...
Goodbye!
```

## Next Steps

Once you've completed this lesson, move on to [Lesson 1.3: Consuming Messages](../lesson-3/README.md) to learn how to read the messages you've just produced!
