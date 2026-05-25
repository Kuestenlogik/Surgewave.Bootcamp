# Lesson 26.2: Producer Optimization

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Lesson 26.1 completion

## Overview

Master producer tuning techniques to maximize throughput while maintaining acceptable latency. Learn how batching, compression, acknowledgment settings, and async patterns affect producer performance.

## Learning Objectives

- Configure batch size and linger time for optimal throughput
- Choose the right compression algorithm for your use case
- Understand acknowledgment levels and their trade-offs
- Implement high-throughput async production patterns
- Tune producer buffer and memory settings

## Producer Configuration Settings

**Batching Settings:**

| Parameter | Description | Trade-off |
|-----------|-------------|-----------|
| `BatchSize` (bytes) | Maximum size of a batch before sending | Small batch + low linger = low latency, lower throughput |
| `LingerMs` (ms) | Time to wait for batch to fill | Large batch + high linger = high throughput, higher latency |

**Compression Settings:**

| Algorithm | Characteristics |
|-----------|----------------|
| None | No CPU overhead, maximum network usage |
| LZ4 | Fast compression, good balance |
| Snappy | Fast, slightly less compression than LZ4 |
| Zstd | Best compression ratio, higher CPU usage |
| Gzip | Good compression, highest CPU usage |

**Acknowledgment Settings:**

| Acks | Behavior |
|------|---------|
| None | Fire-and-forget, fastest, may lose messages |
| Leader | Wait for leader ack, balanced |
| All | Wait for all replicas, safest, slowest |

## Batching Deep Dive

| Mode | Behavior | Network Overhead |
|------|---------|-----------------|
| **Without Batching** (LingerMs=0, BatchSize=1) | Msg1 → Network → Broker → Ack; Msg2 → Network → Broker → Ack; … | HIGH per message |
| **With Batching** (LingerMs=5, BatchSize=16KB) | Collect Msg1, Msg2, …, MsgN → after 5ms or 16KB: [Batch] → Network → Broker → Single Ack | LOW per message |

**Optimal Settings by Use Case:**

| Use Case | BatchSize | LingerMs | Compression |
|----------|-----------|---------|-------------|
| Real-time | 1–4 KB | 0–1 ms | None or LZ4 |
| Balanced | 16–32 KB | 5–10 ms | LZ4 |
| High throughput | 64 KB – 1 MB | 50–100 ms | Zstd |
| Log aggregation | 1 MB | 100 ms | Zstd |

## Async Production Patterns

**Sequential** (slowest):
```csharp
foreach (var msg in messages)
{
    await producer.ProduceAsync(topic, key, value);  // Blocks
}
```

**Fire-and-Forget** (fastest, no guarantees):
```csharp
foreach (var msg in messages)
{
    _ = producer.ProduceAsync(topic, key, value);  // No await
}
await producer.FlushAsync();  // Wait for all at end
```

**Batched Async** (recommended):
```csharp
var tasks = new List<Task>();
foreach (var msg in messages)
{
    tasks.Add(producer.ProduceAsync(topic, key, value));
    if (tasks.Count >= batchSize)
    {
        await Task.WhenAll(tasks);
        tasks.Clear();
    }
}
await Task.WhenAll(tasks);  // Remaining
```

## Compression Comparison

| Algorithm | Compression Ratio | Speed | CPU Usage | Best For |
|-----------|-------------------|-------|-----------|----------|
| None | 1.0x | Fastest | None | Small messages, low CPU |
| LZ4 | 2-4x | Very Fast | Low | Default choice |
| Snappy | 2-3x | Very Fast | Low | Similar to LZ4 |
| Zstd | 4-8x | Fast | Medium | Large messages, logs |
| Gzip | 4-6x | Slow | High | Maximum compression needed |

## Key Configuration Parameters

| Parameter | Description | Default | Recommended Range |
|-----------|-------------|---------|-------------------|
| `BatchSize` | Max bytes per batch | 16KB | 16KB-1MB |
| `LingerMs` | Wait time for batching | 0 | 0-100ms |
| `CompressionType` | Compression algorithm | None | LZ4/Zstd |
| `Acks` | Acknowledgment level | Leader | All for durability |
| `MaxInFlightRequests` | Concurrent requests | 5 | 1-10 |
| `BufferMemory` | Total producer buffer | 32MB | 32-256MB |
| `Retries` | Retry attempts | 3 | 3-10 |

## Exercise

In this exercise, you will:

1. Measure baseline producer performance
2. Test different batch sizes and linger times
3. Compare compression algorithms
4. Implement batched async production
5. Find the optimal configuration for your workload

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
