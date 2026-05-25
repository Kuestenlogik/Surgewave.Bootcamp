# Lesson 7.5: Retry & Backoff Integration

When stream processors call external services (HTTP APIs, databases, third-party systems), transient failures are inevitable. Surgewave's retry integration provides configurable retry policies with multiple backoff strategies.

## Learning Objectives

- Configure `StreamsRetryConfig` with max retries, delays, and strategies
- Choose the right backoff strategy for your use case
- Use `WithRetry` on stream topologies
- Monitor retry metrics and handle exhaustion

## StreamsRetryConfig

```csharp
public class StreamsRetryConfig
{
    public bool Enabled { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public BackoffStrategy Strategy { get; set; } = BackoffStrategy.ExponentialWithJitter;
}
```

## Backoff Strategies

| Strategy | Formula | Example (InitialDelay=100ms) | Use Case |
|----------|---------|------------------------------|----------|
| Fixed | `delay = InitialDelay` | 100, 100, 100 ms | Simple retry, known recovery time |
| Linear | `delay = InitialDelay * attempt` | 100, 200, 300 ms | Gradually increasing backoff |
| Exponential | `delay = InitialDelay * 2^attempt` | 100, 200, 400 ms | API rate limits, server overload |
| ExponentialWithJitter | `delay = random(0, InitialDelay * 2^attempt)` | 73, 156, 312 ms | Multiple clients retrying (prevents thundering herd) |

## Using WithRetry

```csharp
builder.Stream<string, Order>("orders")
    .WithRetry(new StreamsRetryConfig
    {
        Enabled = true,
        MaxRetries = 5,
        InitialDelay = TimeSpan.FromMilliseconds(200),
        MaxDelay = TimeSpan.FromSeconds(10),
        Strategy = BackoffStrategy.ExponentialWithJitter
    })
    .MapAsync(async (key, order) =>
    {
        return await EnrichAsync(order);
    })
    .To("enriched-orders");
```

## RetryNode Processing Flow

The `RetryNode` wrapper catches exceptions and applies the backoff policy. If all retries are exhausted, the record is sent to the dead letter queue (DLQ).

## Retry Metrics

| Metric Name | Type | Description |
|-------------|------|-------------|
| `surgewave.streams.retry.retries_total` | Counter | Total retry attempts |
| `surgewave.streams.retry.retries_exhausted_total` | Counter | Records that exhausted all retries |

## Exercise

1. Create a mock HTTP server that fails 50% of requests with `503 Service Unavailable`
2. Build a stream processor that calls the mock server for each record
3. Configure `WithRetry` using `ExponentialWithJitter` with 3 max retries
4. Process 100 records and verify that all eventually succeed
5. Monitor `retries_total` and `retries_exhausted_total` metrics
6. Reduce `MaxRetries` to 1 and verify some records are sent to the DLQ

---

[Back to Unit 7](../README.md)
