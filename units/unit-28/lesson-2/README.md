# Lesson 3.2: Headers & Metadata

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 3.1

## Overview

Learn how to use message headers for correlation IDs, distributed tracing, and custom metadata.

## Learning Objectives

- Understand message headers and their use cases
- Add and read headers on messages
- Implement correlation IDs for request tracking
- Integrate with distributed tracing

## Why Headers?

Headers allow you to attach metadata to messages without modifying the payload:

| Use Case | Header Example |
|----------|----------------|
| **Correlation** | `x-correlation-id: abc-123` |
| **Tracing** | `traceparent: 00-trace-span-01` |
| **Routing** | `x-target-service: payments` |
| **Versioning** | `x-schema-version: 2` |

## Key Concepts

### Adding Headers

```csharp
var headers = new Headers
{
    { "x-correlation-id", Encoding.UTF8.GetBytes(correlationId) },
    { "x-timestamp", BitConverter.GetBytes(DateTime.UtcNow.Ticks) }
};

await producer.ProduceAsync("topic", key, value, headers);
```

### Reading Headers

```csharp
var result = await consumer.ConsumeAsync(timeout);
var correlationId = result.Headers.GetValueOrDefault("x-correlation-id");
```

## Exercise

In this exercise, you'll:

1. Add correlation IDs to outgoing messages
2. Propagate correlation IDs through a processing pipeline
3. Read and use headers in consumers

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
