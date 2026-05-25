# Knowledge Assessment: Lesson 5.6 - Performance Comparison & Validation

## Multiple Choice

### 1. Which area typically shows the largest performance improvement when migrating from Kafka to Surgewave?

- [ ] A) Throughput (MB/s)
- [ ] B) P99.9 tail latency
- [ ] C) Number of partitions supported
- [ ] D) Topic name length

<details>
<summary>Answer</summary>
**B)** Surgewave's P99.9 latency improvement (~10x) is typically the largest relative gain. JVM GC pauses cause Kafka's tail latency to spike to 100–300 ms, while Surgewave's .NET 10 low-latency GC keeps P99.9 under 20 ms. Throughput improvement (~2x) is significant but smaller in percentage terms.
</details>

### 2. What is the primary reason Surgewave has lower memory usage than Kafka?

- [ ] A) Surgewave uses fewer topics by default
- [ ] B) Surgewave does not support replication
- [ ] C) Surgewave runs on .NET without JVM heap overhead and uses pooled Span<T>/Memory<T> buffers
- [ ] D) Surgewave stores data in a different data centre

<details>
<summary>Answer</summary>
**C)** Kafka's JVM allocates a large heap (800 MB – 2 GB is typical). Surgewave uses .NET's native allocator with pooled `Span<T>` and `Memory<T>` buffers, zero-copy paths via `System.IO.Pipelines`, and no separate ZooKeeper/KRaft process — resulting in a ~10x smaller footprint.
</details>

### 3. What is the most dangerous pitfall when setting `AutoOffsetReset` during migration?

- [ ] A) Setting it to `Earliest` when consumers restart
- [ ] B) Not setting it explicitly and relying on defaults, which may cause missed messages
- [ ] C) Using `Latest` on a new consumer group
- [ ] D) Setting `LingerMs` to 0

<details>
<summary>Answer</summary>
**B)** Not setting `AutoOffsetReset` explicitly is dangerous. If the broker default is `Latest` and a consumer group has no committed offset, it will start from the end of the topic and miss all existing messages. Always set it explicitly for every consumer.
</details>

### 4. You have a Kafka topic with 12 partitions. You create the Surgewave topic with default settings and it gets 1 partition. What is the consequence?

- [ ] A) No consequence — Surgewave automatically re-partitions messages
- [ ] B) Ordering guarantees are improved
- [ ] C) Consumers that relied on partition-level parallelism will process 12x slower
- [ ] D) Producers will error on every message

<details>
<summary>Answer</summary>
**C)** With only 1 partition, consumer group parallelism is limited to a single consumer instance. Applications that used 12 Kafka consumers in parallel for throughput will perform 12x worse. Always pre-create topics with the same partition count as Kafka before directing traffic to Surgewave.
</details>

## True/False

### 5. You can safely keep both `Confluent.Kafka` and `Kuestenlogik.Surgewave.Kafka.Compat` NuGet packages referenced in the same project without namespace conflicts.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** — Both packages export the same `Confluent.Kafka` namespace, which causes compile-time type conflicts. Remove the original `Confluent.Kafka` package and reference only `Kuestenlogik.Surgewave.Kafka.Compat`, which re-exports the same namespace so all existing code continues to compile unchanged.
</details>

### 6. The recommended cutover window starts with switching producers before consumers.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** — The recommended order is:
1. Drain Kafka consumers (wait for consumer lag = 0)
2. Switch consumers to Surgewave
3. Switch producers to Surgewave

Switching consumers first ensures no messages produced into Kafka are missed. Switching producers before consumers could leave Surgewave consumers with no data while Kafka consumers are still draining.
</details>

## Code Challenge

### 7. What is wrong with this benchmark comparison code?

```csharp
var kafkaResult = await RunBenchmarkAsync("localhost:9093", "Kafka", 10_000);
// ... other code ...
await Task.Delay(5000); // Wait 5 seconds for GC
var surgewaveResult = await RunBenchmarkAsync("localhost:9092", "Surgewave", 10_000);

Console.WriteLine($"Surgewave is {kafkaResult.ThroughputMbps / surgewaveResult.ThroughputMbps:F2}x faster");
```

<details>
<summary>Answer</summary>
Two bugs:

1. **Division is inverted.** The ratio `kafkaResult / surgewaveResult` gives a number < 1 if Surgewave is faster. To show "Surgewave is Nx faster", use `surgewaveResult.ThroughputMbps / kafkaResult.ThroughputMbps`.

2. **JIT warm-up not accounted for.** The first benchmark benefits from a cold JIT while the second benefits from a warm JIT, skewing results in Surgewave's favour (it runs second). Run a warm-up pass before both benchmarks.

Fixed version:

```csharp
// Warm-up both brokers equally
await RunBenchmarkAsync("localhost:9093", "warmup-kafka", 500);
await RunBenchmarkAsync("localhost:9092", "warmup-surgewave", 500);

// Real benchmark
var kafkaResult = await RunBenchmarkAsync("localhost:9093", "Kafka", 10_000);
var surgewaveResult = await RunBenchmarkAsync("localhost:9092", "Surgewave", 10_000);

// Correct ratio: Surgewave throughput / Kafka throughput
Console.WriteLine($"Surgewave is {surgewaveResult.ThroughputMbps / kafkaResult.ThroughputMbps:F2}x faster");
Console.WriteLine($"Surgewave P99 is {kafkaResult.P99Ms / surgewaveResult.P99Ms:F2}x lower latency");
```
</details>

### 8. Write a cutover verification function that checks consumer lag on Surgewave after switching and alerts if lag is growing:

<details>
<summary>Answer</summary>

```csharp
public static async Task MonitorPostCutoverAsync(
    string surgewaveBootstrap,
    string topic,
    string groupId,
    TimeSpan duration)
{
    var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = surgewaveBootstrap);
    long previousLag = long.MaxValue;
    int growthCount = 0;
    var deadline = DateTime.UtcNow + duration;

    while (DateTime.UtcNow < deadline)
    {
        var offsets = await admin.ListConsumerGroupOffsetsAsync(groupId);
        long currentLag = 0;

        foreach (var partition in offsets)
        {
            var end = await admin.GetEndOffsetAsync(topic, partition.Partition);
            currentLag += Math.Max(0, end - partition.Offset);
        }

        if (currentLag > previousLag)
        {
            growthCount++;
            Console.WriteLine($"[WARN] Consumer lag growing: {previousLag} → {currentLag}");

            if (growthCount >= 3)
            {
                Console.WriteLine("[ALERT] Lag grew 3 consecutive checks — consider rollback");
                return;
            }
        }
        else
        {
            growthCount = 0;
            Console.WriteLine($"[OK] Consumer lag: {currentLag:N0} (decreasing or stable)");
        }

        previousLag = currentLag;
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    Console.WriteLine("[PASS] Post-cutover monitoring complete — no lag growth detected");
}

// Usage:
await MonitorPostCutoverAsync(
    "localhost:9092", "orders", "order-processor",
    duration: TimeSpan.FromMinutes(30));
```
</details>

## Score

- 8/8: Excellent! Ready to run a production Surgewave migration
- 6-7/8: Good — review the pitfalls and verification checklist
- 5/8: Review the lesson and retry
- <5/8: Re-do the lesson before attempting a real migration
