# Lesson 4.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Baseline Configuration

```csharp
// Default producer (no optimization)
var baselineProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    // All defaults
});
```

## Step 2: High Throughput Configuration

```csharp
var highThroughputProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;

    // Batching: collect more messages before sending
    opt.BatchSize = 65536;        // 64KB batches (default: 16KB)
    opt.LingerMs = 50;            // Wait up to 50ms for batch (default: 0)

    // Compression: reduce network I/O
    opt.CompressionType = CompressionType.Lz4;

    // Buffering: more memory for in-flight data
    opt.BufferMemory = 67108864;  // 64MB buffer (default: 32MB)

    // Acknowledgments: trade durability for speed
    opt.Acks = Acks.One;          // Only wait for leader (default: All)
});
```

## Step 3: Low Latency Configuration

```csharp
var lowLatencyProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;

    // Disable batching: send immediately
    opt.BatchSize = 1;
    opt.LingerMs = 0;

    // No compression: avoid CPU overhead
    opt.CompressionType = CompressionType.None;

    // Acknowledgments: fast ack
    opt.Acks = Acks.One;
});
```

## Step 4: Consumer Throughput Tuning

```csharp
var highThroughputConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;

    // Fetch more data per request
    opt.FetchMinBytes = 65536;           // 64KB minimum
    opt.FetchMaxWaitMs = 500;            // Wait for data
    opt.MaxPartitionFetchBytes = 1048576; // 1MB per partition
});
```

## Step 5: Benchmark Performance

```csharp
async Task<double> BenchmarkProducer(
    SurgewaveProducer<string, string> producer,
    int messageCount,
    string payload)
{
    var sw = Stopwatch.StartNew();

    for (int i = 0; i < messageCount; i++)
    {
        await producer.ProduceAsync("benchmark", $"key-{i}", payload);
    }
    await producer.FlushAsync();

    sw.Stop();

    var messagesPerSecond = messageCount / sw.Elapsed.TotalSeconds;
    return messagesPerSecond;
}

// Compare configurations
var baseline = await BenchmarkProducer(baselineProducer, 100000, payload);
var optimized = await BenchmarkProducer(highThroughputProducer, 100000, payload);

Console.WriteLine($"Baseline: {baseline:N0} msg/sec");
Console.WriteLine($"Optimized: {optimized:N0} msg/sec");
Console.WriteLine($"Improvement: {(optimized/baseline - 1) * 100:F1}%");
```

## Step 6: Broker Tuning

```csharp
var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    // Thread pool sizing
    .WithNumNetworkThreads(8)
    .WithNumIoThreads(16)
    // Memory allocation
    .WithLogSegmentBytes(1073741824)  // 1GB segments
    .WithLogRetentionBytes(10737418240) // 10GB retention
    // Flush tuning
    .WithFlushIntervalMessages(50000)
    .WithFlushIntervalMs(10000)
    .Build()
    .StartAsync();
```

## Tuning Guidelines

| Workload | Batch Size | Linger | Compression | Acks |
|----------|------------|--------|-------------|------|
| High throughput | 64KB+ | 50-100ms | LZ4 | 1 |
| Low latency | Small | 0 | None | 1 |
| High durability | Default | Default | Optional | All |
| Balanced | 32KB | 20ms | Snappy | All |

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Knowledge Check

1. Why does batching improve throughput?
2. When should you disable compression?
3. What's the trade-off with Acks.One vs Acks.All?

---

**Congratulations!** You've completed Unit 21: Production Operations.

**Next Unit:** → [Unit 11: Kafka Migration](../../unit-11/README.md)
