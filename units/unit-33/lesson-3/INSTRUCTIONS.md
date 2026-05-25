# Lesson 26.3: Instructions

Follow these step-by-step instructions to implement consumer optimization techniques.

## Step 1: Measure Baseline Consumer Performance

Start with default settings to establish a baseline:

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "baseline-group";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
consumer.Subscribe(topic);

var metrics = new PerformanceMetrics();
metrics.Start();

var consumed = 0;
while (consumed < messageCount)
{
    var start = Stopwatch.GetTimestamp();
    var result = await consumer.ConsumeAsync();

    if (result?.Value != null)
    {
        var elapsed = Stopwatch.GetElapsedTime(start);
        metrics.RecordMessage(result.Value.Length, elapsed.TotalMilliseconds);
        consumed++;
    }
}

metrics.Stop();
Console.WriteLine($"Baseline: {metrics.MessagesPerSecond:N0} msg/s");
```

## Step 2: Tune Fetch Settings

Adjust fetch size to optimize for your message sizes:

```csharp
var fetchConfigurations = new[]
{
    (Name: "Small Fetch", MinBytes: 1, MaxBytes: 1024 * 16),      // 16KB
    (Name: "Medium Fetch", MinBytes: 1024, MaxBytes: 1024 * 256),  // 256KB
    (Name: "Large Fetch", MinBytes: 1024 * 16, MaxBytes: 1024 * 1024), // 1MB
};

foreach (var config in fetchConfigurations)
{
    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.GroupId = $"fetch-test-{config.Name}";
        opt.FetchMinBytes = config.MinBytes;
        opt.FetchMaxBytes = config.MaxBytes;
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    });
    consumer.Subscribe(topic);

    var metrics = await MeasureConsumerPerformance(consumer, messageCount);
    Console.WriteLine($"{config.Name}: {metrics.MessagesPerSecond:N0} msg/s");
}
```

## Step 3: Implement Parallel Processing with Thread Pool

Process messages concurrently while maintaining single consumer:

```csharp
async Task<PerformanceMetrics> MeasureParallelProcessing(
    SurgewaveConsumer<string, string> consumer,
    int messageCount,
    int parallelism)
{
    var metrics = new PerformanceMetrics();
    var semaphore = new SemaphoreSlim(parallelism);
    var tasks = new List<Task>();
    var consumed = 0;

    metrics.Start();

    while (consumed < messageCount)
    {
        var result = await consumer.ConsumeAsync();
        if (result?.Value == null) continue;

        consumed++;
        await semaphore.WaitAsync();

        var start = Stopwatch.GetTimestamp();
        var messageSize = result.Value.Length;

        var task = Task.Run(async () =>
        {
            try
            {
                // Simulate processing work
                await ProcessMessageAsync(result.Value);

                var elapsed = Stopwatch.GetElapsedTime(start);
                metrics.RecordMessage(messageSize, elapsed.TotalMilliseconds);
            }
            finally
            {
                semaphore.Release();
            }
        });

        tasks.Add(task);
    }

    await Task.WhenAll(tasks);
    metrics.Stop();

    return metrics;
}

async Task ProcessMessageAsync(string message)
{
    // Simulate I/O-bound processing
    await Task.Delay(1);
}
```

## Step 4: Channel-Based Processing with Backpressure

Use channels for decoupled consumption and processing:

```csharp
async Task<PerformanceMetrics> MeasureChannelProcessing(
    SurgewaveConsumer<string, string> consumer,
    int messageCount,
    int channelCapacity,
    int workerCount)
{
    var metrics = new PerformanceMetrics();
    var channel = Channel.CreateBounded<ConsumeResult<string, string>>(
        new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    // Producer task - reads from Kafka
    var producerTask = Task.Run(async () =>
    {
        var consumed = 0;
        while (consumed < messageCount)
        {
            var result = await consumer.ConsumeAsync();
            if (result?.Value != null)
            {
                await channel.Writer.WriteAsync(result);
                consumed++;
            }
        }
        channel.Writer.Complete();
    });

    // Worker tasks - process messages
    metrics.Start();

    var workerTasks = Enumerable.Range(0, workerCount).Select(async _ =>
    {
        await foreach (var result in channel.Reader.ReadAllAsync())
        {
            var start = Stopwatch.GetTimestamp();
            await ProcessMessageAsync(result.Value!);
            var elapsed = Stopwatch.GetElapsedTime(start);
            metrics.RecordMessage(result.Value!.Length, elapsed.TotalMilliseconds);
        }
    });

    await Task.WhenAll(workerTasks.Append(producerTask));
    metrics.Stop();

    return metrics;
}
```

## Step 5: Implement Batch Processing

Process messages in batches for efficiency:

```csharp
async Task<PerformanceMetrics> MeasureBatchProcessing(
    SurgewaveConsumer<string, string> consumer,
    int messageCount,
    int batchSize)
{
    var metrics = new PerformanceMetrics();
    var batch = new List<ConsumeResult<string, string>>(batchSize);
    var consumed = 0;

    metrics.Start();

    while (consumed < messageCount)
    {
        // Collect a batch
        while (batch.Count < batchSize && consumed < messageCount)
        {
            var result = await consumer.ConsumeAsync();
            if (result?.Value != null)
            {
                batch.Add(result);
                consumed++;
            }
        }

        // Process batch
        if (batch.Count > 0)
        {
            var start = Stopwatch.GetTimestamp();
            await ProcessBatchAsync(batch);
            var elapsed = Stopwatch.GetElapsedTime(start);

            foreach (var msg in batch)
            {
                metrics.RecordMessage(msg.Value!.Length, elapsed.TotalMilliseconds / batch.Count);
            }

            batch.Clear();
        }
    }

    metrics.Stop();
    return metrics;
}

async Task ProcessBatchAsync(List<ConsumeResult<string, string>> batch)
{
    // Simulate batch processing (e.g., bulk database insert)
    await Task.Delay(batch.Count / 10); // Faster per message when batched
}
```

## Step 6: Compare All Strategies

Run all strategies and compare:

```csharp
var results = new List<(string Strategy, double Throughput, double P99)>();

// Test sequential
var seqMetrics = await MeasureSequentialProcessing(consumer, messageCount);
results.Add(("Sequential", seqMetrics.MessagesPerSecond, seqMetrics.GetPercentile(99)));

// Test parallel
var parallelMetrics = await MeasureParallelProcessing(consumer, messageCount, parallelism: 10);
results.Add(("Parallel (10)", parallelMetrics.MessagesPerSecond, parallelMetrics.GetPercentile(99)));

// Test channel-based
var channelMetrics = await MeasureChannelProcessing(consumer, messageCount, capacity: 100, workers: 10);
results.Add(("Channel (10 workers)", channelMetrics.MessagesPerSecond, channelMetrics.GetPercentile(99)));

// Test batch processing
var batchMetrics = await MeasureBatchProcessing(consumer, messageCount, batchSize: 100);
results.Add(("Batch (100)", batchMetrics.MessagesPerSecond, batchMetrics.GetPercentile(99)));

// Print comparison
Console.WriteLine("\n--- Strategy Comparison ---");
Console.WriteLine($"{"Strategy",-25} {"Throughput",12} {"P99",10}");
Console.WriteLine(new string('-', 50));

foreach (var (strategy, throughput, p99) in results)
{
    Console.WriteLine($"{strategy,-25} {throughput,9:N0} msg/s {p99,7:F2}ms");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Baseline consumer performance
- Fetch tuning
- Parallel processing patterns
- Channel-based processing with backpressure
- Batch processing
- Strategy comparison

## Sample Output

```
=== Surgewave Bootcamp: Lesson 26.3 - Consumer Optimization ===

Surgewave broker started on port 12345
Produced 20,000 test messages

--- Baseline Consumer Performance ---
Sequential consumption: 8,500 msg/s

--- Fetch Size Comparison ---
Small Fetch (16KB):  7,200 msg/s
Medium Fetch (256KB): 12,400 msg/s
Large Fetch (1MB):   14,100 msg/s

--- Parallel Processing ---
Parallelism=1:  8,500 msg/s
Parallelism=5:  32,400 msg/s
Parallelism=10: 48,200 msg/s
Parallelism=20: 52,100 msg/s

--- Channel-Based Processing ---
Workers=5, Capacity=50:  35,200 msg/s
Workers=10, Capacity=100: 56,800 msg/s

--- Batch Processing ---
BatchSize=10:  15,600 msg/s
BatchSize=50:  42,300 msg/s
BatchSize=100: 58,400 msg/s

--- Strategy Comparison ---
Strategy                   Throughput        P99
--------------------------------------------------
Sequential                     8,500 msg/s   0.12ms
Parallel (10)                 48,200 msg/s   0.45ms
Channel (10 workers)          56,800 msg/s   0.38ms
Batch (100)                   58,400 msg/s   1.25ms

Lesson complete!
```

## Knowledge Check

1. What is the relationship between partition count and maximum useful consumers?
2. How does FetchMinBytes affect consumption latency?
3. When should you use channel-based processing vs parallel processing?
4. How do you handle slow message processing without rebalances?
5. What are the trade-offs of batch processing vs individual message processing?

---

**Next:** [Lesson 26.4: Memory Management & GC Tuning](../lesson-4/README.md)
