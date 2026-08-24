# Lesson 5.6: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Start Two Brokers

```csharp
await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

await using var kafka = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9093)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine("Surgewave: localhost:9092 | Kafka (sim): localhost:9093");
```

## Step 2: Implement a Mini Benchmark

```csharp
public static async Task<BenchmarkResult> RunBenchmarkAsync(
    string bootstrap, string label, int messageCount = 10_000)
{
    await using var producer = new SurgewaveProducer<string, byte[]>(opt =>
    {
        opt.BootstrapServers = bootstrap;
        opt.LingerMs = 5;
    });

    var payload = new byte[1024]; // 1 KB messages
    Random.Shared.NextBytes(payload);

    var latencies = new List<double>(messageCount);
    var sw = Stopwatch.StartNew();

    for (int i = 0; i < messageCount; i++)
    {
        var msgSw = Stopwatch.StartNew();
        await producer.ProduceAsync("bench", $"k-{i}", payload);
        latencies.Add(msgSw.Elapsed.TotalMilliseconds);
    }

    sw.Stop();
    latencies.Sort();

    double throughput = (messageCount * 1024.0) / sw.Elapsed.TotalSeconds / (1024.0 * 1024.0);
    double p50 = latencies[(int)(messageCount * 0.50)];
    double p99 = latencies[(int)(messageCount * 0.99)];

    Console.WriteLine($"[{label}] {messageCount:N0} messages | " +
                      $"Throughput: {throughput:F1} MB/s | " +
                      $"P50: {p50:F2} ms | P99: {p99:F2} ms");

    return new BenchmarkResult(label, throughput, p50, p99);
}

public record BenchmarkResult(string Label, double ThroughputMbps, double P50Ms, double P99Ms);
```

## Step 3: Run and Compare

```csharp
Console.WriteLine("--- Running Benchmarks ---");

var kafkaResult = await RunBenchmarkAsync("localhost:9093", "Kafka", 10_000);
var surgewaveResult = await RunBenchmarkAsync("localhost:9092", "Surgewave", 10_000);

Console.WriteLine();
Console.WriteLine("--- Comparison ---");
Console.WriteLine($"Throughput:  Surgewave {surgewaveResult.ThroughputMbps / kafkaResult.ThroughputMbps:F2}x " +
                  $"({'faster' if throughput better})");
Console.WriteLine($"P50 latency: Surgewave {kafkaResult.P50Ms / surgewaveResult.P50Ms:F2}x faster");
Console.WriteLine($"P99 latency: Surgewave {kafkaResult.P99Ms / surgewaveResult.P99Ms:F2}x faster");
```

## Step 4: Reproduce and Fix Common Pitfalls

### Pitfall 1 — Wrong batch size default

```csharp
// Wrong: copying Kafka's 16 KB batch
var badConfig = new SurgewaveProducerOptions { BatchSize = 16384 };

// Correct: Surgewave performs better with larger batches
var goodConfig = new SurgewaveProducerOptions { BatchSize = 131072, LingerMs = 5 };
```

### Pitfall 2 — Partition count mismatch

```csharp
// Before cutover, pre-create topics matching Kafka's partition layout
var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = "localhost:9092");
await admin.CreateTopicsAsync(
[
    new TopicSpecification { Name = "orders", NumPartitions = 12, ReplicationFactor = 1 }
]);
Console.WriteLine("Pre-created 'orders' with 12 partitions (matching Kafka)");
```

### Pitfall 3 — AutoOffsetReset not set explicitly

```csharp
// Dangerous: relying on defaults
var dangerous = new SurgewaveConsumerOptions
{
    BootstrapServers = "localhost:9092",
    GroupId = "my-group"
    // AutoOffsetReset not set — might miss messages!
};

// Safe: always explicit
var safe = new SurgewaveConsumerOptions
{
    BootstrapServers = "localhost:9092",
    GroupId = "my-group",
    AutoOffsetReset = AutoOffsetReset.Earliest // Never miss a message
};
```

## Step 5: Run the Cutover Checklist

```csharp
public static async Task RunCutoverChecklist(string surgewaveBootstrap, string topic)
{
    Console.WriteLine("=== Pre-Cutover Checklist ===");

    // Check 1: Broker reachable
    try
    {
        var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = surgewaveBootstrap);
        var meta = await admin.GetMetadataAsync(TimeSpan.FromSeconds(5));
        Console.WriteLine($"[PASS] Surgewave broker reachable ({meta.Brokers.Count} broker(s))");
    }
    catch
    {
        Console.WriteLine("[FAIL] Surgewave broker not reachable");
        return;
    }

    // Check 2: Topic exists with correct partition count
    try
    {
        var admin = new SurgewaveAdminClient(opt => opt.BootstrapServers = surgewaveBootstrap);
        var meta = await admin.GetTopicMetadataAsync(topic);
        Console.WriteLine($"[PASS] Topic '{topic}' exists with {meta.Partitions.Count} partition(s)");
    }
    catch
    {
        Console.WriteLine($"[WARN] Topic '{topic}' not found — will be auto-created");
    }

    // Check 3: Produce and consume round-trip
    try
    {
        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = surgewaveBootstrap);
        await using var consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = surgewaveBootstrap;
            opt.GroupId = $"checklist-{Guid.NewGuid()}";
            opt.AutoOffsetReset = AutoOffsetReset.Latest;
        });

        consumer.Subscribe(topic);
        var sentinel = $"checklist-{DateTime.UtcNow:O}";
        await producer.ProduceAsync(topic, "checklist", sentinel);

        var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(3));
        Console.WriteLine(result?.Value == sentinel
            ? "[PASS] Produce/consume round-trip verified"
            : "[WARN] Round-trip message not matched — check consumer group offset");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] Round-trip failed: {ex.Message}");
    }

    Console.WriteLine("=== Checklist Complete ===");
}
```

## Step 6: Print Migration Summary

```csharp
Console.WriteLine();
Console.WriteLine("=== Migration Summary ===");
Console.WriteLine($"  Producer throughput improvement:  {surgewaveResult.ThroughputMbps / kafkaResult.ThroughputMbps:F1}x");
Console.WriteLine($"  P99 latency improvement:          {kafkaResult.P99Ms / surgewaveResult.P99Ms:F1}x");
Console.WriteLine($"  Startup time:                     Surgewave ~0.6s vs Kafka ~15s");
Console.WriteLine($"  Memory footprint:                 Surgewave ~100 MB vs Kafka ~1 GB");
Console.WriteLine();
Console.WriteLine("  Recommendation: Surgewave is ready for production cutover.");
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

---

**Next:** → [Unit 5: Stream Processing](../../unit-5/README.md)
