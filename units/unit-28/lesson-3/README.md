# Lesson 3.3: Error Handling

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 3.2

## Overview

Learn how to handle failures gracefully with retries, dead letter queues, and circuit breakers.

## Learning Objectives

- Implement retry logic for transient failures
- Use dead letter queues for poison messages
- Apply circuit breaker patterns
- Build resilient message processing pipelines

## Why Error Handling Matters

In distributed systems, failures are inevitable:

| Failure Type | Example | Solution |
|--------------|---------|----------|
| **Transient** | Network timeout | Retry with backoff |
| **Permanent** | Invalid data | Dead letter queue |
| **Cascading** | Downstream outage | Circuit breaker |

## Key Patterns

### Retry with Exponential Backoff

```csharp
async Task ProcessWithRetry(Message msg, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await ProcessMessage(msg);
            return;
        }
        catch (Exception) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            await Task.Delay(delay);
        }
    }
    await SendToDeadLetterQueue(msg);
}
```

### Dead Letter Queue (DLQ)

```csharp
catch (ProcessingException ex)
{
    await producer.ProduceAsync(
        "orders.dlq",
        msg.Key,
        msg.Value,
        new Headers { { "x-error", ex.Message } });
}
```

## Native DLQ with Nack API

In addition to the application-level DLQ pattern shown above, Surgewave provides a **native Nack API** at the broker level. When a consumer calls `consumer.Nack(message)`, the broker automatically routes the message to the topic's configured dead letter queue (`dead.letter.queue.topic`). The broker attaches metadata headers (`x-dlq-reason`, `x-dlq-timestamp`, `x-original-topic`) to nacked messages. This eliminates the need for the application to manually produce to a DLQ topic on failure.

## Per-Message TTL

Surgewave supports a `surgewave-ttl-ms` header for per-message time-to-live. Expired messages are automatically skipped during consumption. This complements DLQ handling by ensuring stale poison messages do not accumulate indefinitely.

## Exercise

In this exercise, you'll:

1. Implement retry logic with exponential backoff
2. Create a dead letter queue for failed messages
3. Use the native Nack API for broker-level DLQ routing
4. Track error metrics

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
