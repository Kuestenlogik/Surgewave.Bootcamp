# Lesson 5.6: Performance Comparison & Validation

> **Difficulty:** Intermediate | **Duration:** 50 min | **Prerequisites:** Lesson 5.5

## Overview

Learn how to benchmark Surgewave against Kafka, interpret performance differences, avoid common
migration pitfalls, and validate that your production cutover is safe.

## Learning Objectives

- Run benchmarks comparing Surgewave and Kafka throughput and latency
- Understand expected performance differences and what drives them
- Identify and fix common Confluent.Kafka → Surgewave client migration issues
- Execute a verification checklist before production cutover

## Benchmarking Surgewave vs Kafka

Surgewave ships with a built-in benchmark tool. Run it against both brokers to compare:

```bash
# Benchmark Surgewave (port 9092)
dotnet run --project benchmarks/Kuestenlogik.Surgewave.Benchmarks.RealWorld -- \
  --bootstrap localhost:9092 \
  --topic bench-surgewave \
  --messages 1000000 \
  --batch-size 1000

# Benchmark Kafka (port 9093)
dotnet run --project benchmarks/Kuestenlogik.Surgewave.Benchmarks.RealWorld -- \
  --bootstrap localhost:9093 \
  --topic bench-kafka \
  --messages 1000000 \
  --batch-size 1000
```

Or use the `surgewave-cli` benchmark command against either broker:

```bash
# Producer benchmark: 1M messages, 1 KB each
surgewave-cli benchmark producer \
  --bootstrap localhost:9092 \
  --messages 1000000 \
  --message-size 1024 \
  --acks all \
  --report throughput,latency

# Consumer benchmark: consume all messages from the topic
surgewave-cli benchmark consumer \
  --bootstrap localhost:9092 \
  --topic bench-surgewave \
  --group bench-group \
  --report throughput,lag
```

## Expected Performance Differences

Surgewave is designed to beat Kafka in every category. Typical results on the same hardware:

| Metric | Kafka (JVM) | Surgewave (.NET) | Surgewave Advantage |
|--------|-------------|--------------|-----------------|
| Producer throughput | ~800 MB/s | ~1.8 GB/s | ~2.2x |
| Consumer throughput | ~600 MB/s | ~1.6 GB/s | ~2.7x |
| P50 produce latency | 4–8 ms | 0.5–1.2 ms | ~5x |
| P99 produce latency | 20–60 ms | 2–6 ms | ~7x |
| P99.9 produce latency | 100–300 ms | 8–20 ms | ~10x |
| Startup time | 10–20 s | 0.4–0.8 s | ~25x |
| Memory footprint | 800 MB–2 GB | 80–150 MB | ~10x |

> **Note:** Numbers vary by hardware, message size, replication factor, and ack settings.
> Run benchmarks in your own environment — do not rely solely on these estimates.

### Why is Surgewave Faster?

```
Kafka                          Surgewave
─────────────────              ─────────────────────────────────
JVM GC pauses (10-300ms)  vs  .NET 10 low-latency GC (~0.5ms)
Java heap overhead        vs  Span<T>/Memory<T> zero-copy paths
ZooKeeper/KRaft overhead  vs  Built-in Raft (no extra process)
Thread-per-request model  vs  System.IO.Pipelines async I/O
PageCache writes          vs  Arrow columnar + mmap storage
```

## Running a Side-by-Side Benchmark

```csharp
public class BenchmarkRunner
{
    private readonly string _surgewaveBootstrap;
    private readonly string _kafkaBootstrap;

    public async Task<BenchmarkResult> RunProducerBenchmarkAsync(
        string bootstrap, string topic, int messageCount, int batchSize)
    {
        await using var producer = new SurgewaveProducer<string, byte[]>(opt =>
        {
            opt.BootstrapServers = bootstrap;
            opt.LingerMs = 5;                // Batching
            opt.BatchSize = batchSize * 1024; // Batch in bytes
        });

        var payload = new byte[1024]; // 1 KB message
        Random.Shared.NextBytes(payload);

        var sw = Stopwatch.StartNew();
        var latencies = new List<double>(messageCount);

        for (int i = 0; i < messageCount; i++)
        {
            var msgSw = Stopwatch.StartNew();
            await producer.ProduceAsync(topic, $"key-{i}", payload);
            latencies.Add(msgSw.Elapsed.TotalMilliseconds);
        }

        sw.Stop();
        latencies.Sort();

        return new BenchmarkResult(
            TotalMessages: messageCount,
            DurationMs: sw.ElapsedMilliseconds,
            ThroughputMbps: (messageCount * 1024.0) / sw.Elapsed.TotalSeconds / (1024 * 1024),
            P50Ms: latencies[(int)(messageCount * 0.50)],
            P99Ms: latencies[(int)(messageCount * 0.99)],
            P999Ms: latencies[(int)(messageCount * 0.999)]
        );
    }

    public void PrintComparison(BenchmarkResult surgewave, BenchmarkResult kafka)
    {
        Console.WriteLine($"{"Metric",-25} {"Kafka",12} {"Surgewave",12} {"Surgewave Win",10}");
        Console.WriteLine(new string('-', 61));
        PrintRow("Throughput (MB/s)", kafka.ThroughputMbps, surgewave.ThroughputMbps, higherIsBetter: true);
        PrintRow("P50 latency (ms)", kafka.P50Ms, surgewave.P50Ms, higherIsBetter: false);
        PrintRow("P99 latency (ms)", kafka.P99Ms, surgewave.P99Ms, higherIsBetter: false);
        PrintRow("P99.9 latency (ms)", kafka.P999Ms, surgewave.P999Ms, higherIsBetter: false);
    }

    private static void PrintRow(string name, double kafka, double surgewave, bool higherIsBetter)
    {
        double ratio = higherIsBetter ? surgewave / kafka : kafka / surgewave;
        Console.WriteLine($"{name,-25} {kafka,12:F2} {surgewave,12:F2} {ratio,8:F1}x");
    }
}

public record BenchmarkResult(
    int TotalMessages,
    long DurationMs,
    double ThroughputMbps,
    double P50Ms,
    double P99Ms,
    double P999Ms);
```

## Common Migration Pitfalls and Fixes

### Pitfall 1: Acks Configuration Mismatch

```csharp
// Kafka default: acks=1 (leader only)
// Surgewave default: acks=all (strongly recommended)
// If you copy Kafka config verbatim, acks=1 works — but you miss Surgewave's stronger guarantees

// Before (Kafka config)
new ProducerConfig { Acks = Acks.Leader };

// After (Surgewave recommended — maintains durability)
new SurgewaveProducerOptions { Acks = AcksMode.All };
```

### Pitfall 2: Wrong Batch / Linger Settings

```csharp
// Kafka: LingerMs=0 for low latency; BatchSize=16384 (16 KB)
// Surgewave: higher defaults are safe and improve throughput significantly

// Before
new ProducerConfig { LingerMs = 0, BatchSize = 16384 };

// After (Surgewave-optimised for throughput)
new SurgewaveProducerOptions
{
    LingerMs = 5,        // Small linger improves batching dramatically
    BatchSize = 131072   // 128 KB — Surgewave handles large batches efficiently
};
```

### Pitfall 3: Serializer Namespace Collisions

```csharp
// If you keep Confluent.Kafka NuGet alongside Kuestenlogik.Surgewave.Kafka.Compat, namespaces conflict.
// Solution: remove Confluent.Kafka NuGet entirely and use the compat shim only.

// Remove from .csproj:
// <PackageReference Include="Confluent.Kafka" Version="2.x.x" />

// Add instead:
// <PackageReference Include="Kuestenlogik.Surgewave.Kafka.Compat" />

// Code is unchanged — Kuestenlogik.Surgewave.Kafka.Compat exports Confluent.Kafka namespaces
using Confluent.Kafka; // Now provided by Surgewave.Kafka.Compat
```

### Pitfall 4: Consumer Group Auto-Offset-Reset Differences

```csharp
// Kafka default: auto.offset.reset = latest (miss messages if consumer restarts)
// Surgewave default: AutoOffsetReset.Latest (same)
// Ensure Earliest is set when you want to replay all messages

new SurgewaveConsumerOptions
{
    AutoOffsetReset = AutoOffsetReset.Earliest // Explicit, don't rely on defaults
};
```

### Pitfall 5: Partition Count Mismatch

```csharp
// Kafka topics may have been created with specific partition counts.
// Surgewave will auto-create topics but with its default (often 1).
// Pre-create topics with the same partition count as Kafka.

var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = "localhost:9092");
await admin.CreateTopicsAsync(
[
    new TopicSpecification
    {
        Name = "orders",
        NumPartitions = 12,    // Match Kafka partition count
        ReplicationFactor = 3
    }
]);
```

### Pitfall 6: Transaction API Differences

```csharp
// Confluent.Kafka: BeginTransaction / CommitTransaction / AbortTransaction
// Surgewave: same API, compatible — but verify idempotence settings

new SurgewaveProducerOptions
{
    TransactionalId = "my-transactional-producer",
    EnableIdempotence = true  // Required for transactions
};
// InitTransactionsAsync / BeginTransaction / CommitTransactionAsync — identical API
```

## Verification Checklist for Production Cutover

### Pre-Cutover (T-7 days)

```
□ Surgewave cluster deployed and sized to handle peak load + 30% headroom
□ Cluster Link running and lag < 500 ms for all topics
□ All consumer group offsets synced (ConsumerGroupSync = true)
□ Load test run: Surgewave handled peak throughput without error for 4+ hours
□ P99 latency within SLA under load test conditions
□ Monitoring dashboards configured (Prometheus / Grafana / Surgewave metrics endpoint)
□ Rollback procedure documented and tested in staging
□ Stakeholders notified of maintenance window
□ On-call rotation scheduled for 48h post-cutover
```

### Cutover Day (T-0)

```
□ Announce maintenance window / put app in degraded mode if possible
□ Drain Kafka consumers (wait for lag = 0)
□ Pause non-critical producers
□ Switch consumer bootstrap servers to Surgewave
□ Switch producer bootstrap servers to Surgewave
□ Verify first messages flowing through Surgewave in monitoring
□ Confirm consumer lag is decreasing (not growing)
□ Check error rates in application logs
□ Verify downstream systems receiving data correctly
□ Remove Cluster Links (Surgewave is now standalone)
```

### Post-Cutover Burn-In (T+1 to T+30 days)

```
□ T+1:  Review P99 latency — within SLA?
□ T+1:  Consumer lag stable and low?
□ T+7:  No anomalous errors in 7-day window?
□ T+7:  Message counts reconciled (Surgewave == Kafka historical)?
□ T+14: Peak load event handled without incident?
□ T+30: Begin Kafka decommission (snapshot, then shutdown)
□ T+30: Update runbooks and architecture diagrams
```

## Client Configuration Reference

| Confluent.Kafka Property | Surgewave Equivalent | Notes |
|--------------------------|-----------------|-------|
| `bootstrap.servers` | `BootstrapServers` | Identical |
| `group.id` | `GroupId` | Identical |
| `acks` | `Acks` | Identical values (0, 1, all) |
| `linger.ms` | `LingerMs` | Identical |
| `batch.size` | `BatchSize` | Identical, Surgewave default higher |
| `auto.offset.reset` | `AutoOffsetReset` | Identical |
| `enable.auto.commit` | `EnableAutoCommit` | Identical |
| `transactional.id` | `TransactionalId` | Identical |
| `compression.type` | `CompressionType` | Identical (none/gzip/lz4/zstd) |
| `max.poll.records` | `MaxPollRecords` | Identical |

## Exercise

In this lesson, you'll:

1. Start two brokers representing Kafka and Surgewave
2. Run a producer benchmark against each and compare throughput and latency
3. Reproduce and fix the three most common migration pitfalls
4. Walk through the pre-cutover checklist programmatically

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)
