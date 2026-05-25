# Lesson 26.1: Performance Fundamentals

> **Difficulty:** Advanced | **Duration:** 45 min | **Prerequisites:** Unit 21 completion

## Overview

Learn the fundamental performance concepts in distributed messaging systems, including throughput, latency metrics, percentile analysis, and how to identify bottlenecks in Surgewave applications.

## Learning Objectives

- Understand throughput and its measurement in messages/second and bytes/second
- Master latency metrics: P50, P90, P99, and P99.9 percentiles
- Learn to identify common performance bottlenecks
- Implement basic performance measurement in Surgewave applications
- Calculate and interpret performance statistics

## Throughput vs Latency

| Mode | Batch Size | Linger | Compression | Result |
|------|-----------|--------|-------------|--------|
| **High Throughput** | 64 KB – 1 MB | 50–100 ms | LZ4 / Zstd | More messages/sec, higher per-message latency |
| **Low Latency** | 1–16 KB | 0 or minimal | None or LZ4 | Lower latency, reduced throughput |
| **Balanced** | 16–64 KB | 1–5 ms | LZ4 | Good throughput with acceptable latency |

## Understanding Percentiles

Example: 1,000 requests sorted by latency:

| Requests | Latency | Percentile |
|----------|---------|-----------|
| 1–500 | < 1 ms | P50 = 1 ms (50% of requests) |
| 501–900 | 1–5 ms | P90 = 5 ms (90% of requests) |
| 901–990 | 5–10 ms | P99 = 10 ms (99% of requests) |
| 991–999 | 10–50 ms | P99.9 = 50 ms (99.9% of requests) |
| 1000 | 100 ms | Max = 100 ms (worst case) |

**Why P99 matters:** 1% of 1M requests/day = 10,000 slow requests; users experience tail latency during high load; SLA breaches often triggered by P99/P99.9; GC pauses typically appear in tail latencies.

## Common Bottlenecks

**Producer Bottlenecks:**

| Symptom | Cause | Solution |
|---------|-------|---------|
| Low throughput | Small batches | Increase BatchSize |
| High latency | Sync calls | Use async/await |
| Memory pressure | No buffer pooling | Enable memory pools |
| CPU spikes | Compression | Use LZ4 or disable |

**Consumer Bottlenecks:**

| Symptom | Cause | Solution |
|---------|-------|---------|
| Lag growing | Slow processing | Increase parallelism |
| Rebalances | Long poll intervals | Tune MaxPollInterval |
| Memory spikes | Large fetches | Reduce FetchMaxBytes |
| Idle consumers | Too few partitions | Increase partitions |

**Broker Bottlenecks:**

| Symptom | Cause | Solution |
|---------|-------|---------|
| Disk I/O bound | Slow storage | Use NVMe SSD |
| Network saturated | High message volume | Add brokers |
| CPU bound | Too many partitions | Consolidate topics |

## Measurement Techniques

**Throughput Measurement:**
```csharp
var stopwatch = Stopwatch.StartNew();
var messageCount = 0;
var byteCount = 0L;

// Produce/consume messages...
messageCount++;
byteCount += messageSize;

stopwatch.Stop();
var msgsPerSec = messageCount / stopwatch.Elapsed.TotalSeconds;
var mbPerSec = byteCount / 1024.0 / 1024.0 / elapsed.TotalSeconds;
```

**Latency Measurement:**
```csharp
var latencies = new List<double>();

var start = Stopwatch.GetTimestamp();
await producer.ProduceAsync(...);
var elapsed = Stopwatch.GetElapsedTime(start);
latencies.Add(elapsed.TotalMilliseconds);

// Calculate percentiles
latencies.Sort();
var p50 = latencies[(int)(latencies.Count * 0.50)];
var p99 = latencies[(int)(latencies.Count * 0.99)];
```

## Key Metrics to Track

| Metric | Description | Target |
|--------|-------------|--------|
| Messages/sec | Producer/consumer throughput | 100K-1M+ |
| MB/sec | Data throughput | 100-500+ MB/s |
| P50 latency | Median latency | <1ms |
| P99 latency | Tail latency | <10ms |
| P99.9 latency | Extreme tail | <50ms |
| GC pause time | Memory pressure indicator | <1ms |
| Consumer lag | Processing backlog | <1000 messages |
| Rebalance time | Consumer group stability | <10 seconds |

## Exercise

In this exercise, you will:

1. Implement throughput measurement for producers and consumers
2. Calculate latency percentiles (P50, P90, P99)
3. Identify bottlenecks in a sample application
4. Compare performance with different configurations
5. Generate a performance report

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)
