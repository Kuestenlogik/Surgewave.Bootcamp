# Lesson 26.2: Instructions

Follow these step-by-step instructions to implement producer optimization techniques.

## Step 1: Create Baseline Measurement

First, measure performance with default settings:

```csharp
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    // Default settings - no optimization
});

var metrics = await MeasureProducerPerformance(producer, topic, messageCount, messageSize);
Console.WriteLine($"Baseline: {metrics.MessagesPerSecond:N0} msg/s, P99: {metrics.GetPercentile(99):F2}ms");
```

## Step 2: Test Batch Size Variations

Compare different batch sizes:

```csharp
var batchSizes = new[] { 1024, 16384, 65536, 262144 }; // 1KB, 16KB, 64KB, 256KB

foreach (var batchSize in batchSizes)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = batchSize;
        opt.LingerMs = 5; // Fixed linger for comparison
    });

    var metrics = await MeasureProducerPerformance(producer, topic, messageCount, messageSize);
    Console.WriteLine($"BatchSize={batchSize/1024}KB: {metrics.MessagesPerSecond:N0} msg/s");
}
```

## Step 3: Test Linger Time Variations

Compare different linger times:

```csharp
var lingerTimes = new[] { 0, 1, 5, 10, 50, 100 };

foreach (var lingerMs in lingerTimes)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = 65536;
        opt.LingerMs = lingerMs;
    });

    var metrics = await MeasureProducerPerformance(producer, topic, messageCount, messageSize);
    Console.WriteLine($"LingerMs={lingerMs}: {metrics.MessagesPerSecond:N0} msg/s, P50: {metrics.GetPercentile(50):F2}ms");
}
```

## Step 4: Compare Compression Algorithms

Test each compression type:

```csharp
var compressionTypes = new[]
{
    ("None", CompressionType.None),
    ("LZ4", CompressionType.Lz4),
    ("Snappy", CompressionType.Snappy),
    ("Zstd", CompressionType.Zstd),
    ("Gzip", CompressionType.Gzip)
};

foreach (var (name, compression) in compressionTypes)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.CompressionType = compression;
        opt.BatchSize = 65536;
    });

    var metrics = await MeasureProducerPerformance(producer, topic, messageCount, messageSize);
    Console.WriteLine($"{name}: {metrics.MessagesPerSecond:N0} msg/s, {metrics.MegabytesPerSecond:F2} MB/s");
}
```

## Step 5: Implement Batched Async Pattern

Use concurrent task batching for higher throughput:

```csharp
async Task<PerformanceMetrics> MeasureAsyncBatchedProduction(
    SurgewaveProducer<string, string> producer,
    string topic,
    int messageCount,
    int messageSize,
    int concurrency = 100)
{
    var metrics = new PerformanceMetrics();
    var message = new string('x', messageSize);
    var tasks = new List<Task>(concurrency);

    metrics.Start();

    for (int i = 0; i < messageCount; i++)
    {
        var start = Stopwatch.GetTimestamp();
        var task = producer.ProduceAsync(topic, $"key-{i}", message)
            .ContinueWith(_ =>
            {
                var elapsed = Stopwatch.GetElapsedTime(start);
                metrics.RecordMessage(messageSize, elapsed.TotalMilliseconds);
            });

        tasks.Add(task);

        // Batch and await when reaching concurrency limit
        if (tasks.Count >= concurrency)
        {
            await Task.WhenAll(tasks);
            tasks.Clear();
        }
    }

    // Process remaining
    if (tasks.Count > 0)
    {
        await Task.WhenAll(tasks);
    }

    metrics.Stop();
    return metrics;
}
```

## Step 6: Find Optimal Configuration

Test combinations to find the best settings:

```csharp
var configurations = new[]
{
    new ProducerConfig("Low Latency", 4096, 0, CompressionType.None),
    new ProducerConfig("Balanced", 32768, 5, CompressionType.Lz4),
    new ProducerConfig("High Throughput", 131072, 50, CompressionType.Zstd),
};

var results = new List<(string Name, double Throughput, double P99)>();

foreach (var config in configurations)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = config.BatchSize;
        opt.LingerMs = config.LingerMs;
        opt.CompressionType = config.Compression;
    });

    var metrics = await MeasureAsyncBatchedProduction(producer, topic, messageCount, messageSize);
    results.Add((config.Name, metrics.MessagesPerSecond, metrics.GetPercentile(99)));
}

// Print comparison table
Console.WriteLine("\n--- Configuration Comparison ---");
Console.WriteLine($"{"Config",-20} {"Throughput",15} {"P99 Latency",15}");
Console.WriteLine(new string('-', 52));

foreach (var (name, throughput, p99) in results)
{
    Console.WriteLine($"{name,-20} {throughput,12:N0} msg/s {p99,12:F2} ms");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Baseline measurements
- Batch size optimization
- Linger time tuning
- Compression comparison
- Async batched production
- Optimal configuration finding

## Sample Output

```
=== Surgewave Bootcamp: Lesson 26.2 - Producer Optimization ===

Surgewave broker started on port 12345

--- Baseline Performance ---
Default settings: 25,430 msg/s, P99: 0.45ms

--- Batch Size Comparison ---
BatchSize=1KB:   18,250 msg/s
BatchSize=16KB:  42,300 msg/s
BatchSize=64KB:  68,500 msg/s
BatchSize=256KB: 72,100 msg/s

--- Linger Time Comparison ---
LingerMs=0:   45,200 msg/s, P50: 0.02ms
LingerMs=5:   62,400 msg/s, P50: 2.50ms
LingerMs=50:  85,300 msg/s, P50: 25.10ms

--- Compression Comparison ---
None:   68,500 msg/s, 66.89 MB/s
LZ4:    65,200 msg/s, 63.67 MB/s (actual: 21.2 MB/s on wire)
Zstd:   58,100 msg/s, 56.74 MB/s (actual: 11.4 MB/s on wire)

--- Async Batched Production ---
Concurrency=10:  45,600 msg/s
Concurrency=100: 125,400 msg/s
Concurrency=500: 148,200 msg/s

--- Configuration Comparison ---
Config               Throughput    P99 Latency
----------------------------------------------------
Low Latency             45,200 msg/s         0.15 ms
Balanced                85,300 msg/s         8.50 ms
High Throughput        148,200 msg/s        55.20 ms

Lesson complete!
```

## Knowledge Check

1. How does batch size affect throughput vs latency?
2. When should you use Zstd compression vs LZ4?
3. What is the trade-off with higher linger times?
4. Why is async batched production faster than sequential?
5. How do you choose the right configuration for real-time systems?

---

**Next:** [Lesson 26.3: Consumer Optimization](../lesson-3/README.md)
