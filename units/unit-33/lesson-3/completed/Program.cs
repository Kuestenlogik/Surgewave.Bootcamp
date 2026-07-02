// Surgewave Bootcamp - Unit 26, Lesson 3: Consumer Optimization
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Threading.Channels;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.3 - Consumer Optimization ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var bootstrapServers = runtime.BootstrapServers;
var topic = "consumer-optimization-test";

// Configuration
var messageCount = 5_000;
var messageSizeBytes = 512;

// Step 1 - Pre-populate topic with test messages
Console.WriteLine($"\nProducing {messageCount:N0} test messages...");
await using var seedProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

for (int i = 0; i < messageCount; i++)
{
    await seedProducer.ProduceAsync(topic, $"key-{i}", new string('x', messageSizeBytes));
}
Console.WriteLine("Test messages produced.");

// Helper: produce messages to a specific topic for each test
async Task ProduceTestMessages(string targetTopic, int count)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
    });
    for (int i = 0; i < count; i++)
    {
        await producer.ProduceAsync(targetTopic, $"key-{i}", new string('x', messageSizeBytes));
    }
}

// Step 2 - Measure baseline consumer performance (sequential)
Console.WriteLine("\n--- Baseline Consumer Performance ---");

var baselineMetrics = new PerformanceMetrics();
await using var baselineConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "baseline-group";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
baselineConsumer.Subscribe(topic);

baselineMetrics.Start();
var consumed = 0;
using var baselineCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    while (consumed < messageCount && !baselineCts.Token.IsCancellationRequested)
    {
        var start = Stopwatch.GetTimestamp();
        var result = await baselineConsumer.ConsumeAsync(baselineCts.Token);
        if (result?.Value != null)
        {
            // Simulate processing
            await ProcessMessageAsync(result.Value);
            var elapsed = Stopwatch.GetElapsedTime(start);
            baselineMetrics.RecordMessage(result.Value.Length, elapsed.TotalMilliseconds);
            consumed++;
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine($"Timeout after consuming {consumed} messages");
}

baselineMetrics.Stop();
Console.WriteLine($"Sequential: {baselineMetrics.MessagesPerSecond:N0} msg/s, P99: {baselineMetrics.GetPercentile(99):F3}ms");

// Step 3 - Parallel processing with SemaphoreSlim
Console.WriteLine("\n--- Parallel Processing ---");

var parallelismLevels = new[] { 1, 5, 10, 20 };
var results = new List<(string Strategy, double Throughput, double P99)>();
results.Add(("Sequential", baselineMetrics.MessagesPerSecond, baselineMetrics.GetPercentile(99)));

foreach (var parallelism in parallelismLevels)
{
    var testTopic = $"{topic}-parallel-{parallelism}";
    await ProduceTestMessages(testTopic, messageCount);

    var metrics = await MeasureParallelProcessing(testTopic, parallelism);
    Console.WriteLine($"Parallelism={parallelism,-3}: {metrics.MessagesPerSecond,8:N0} msg/s, P99: {metrics.GetPercentile(99):F3}ms");
    results.Add(($"Parallel ({parallelism})", metrics.MessagesPerSecond, metrics.GetPercentile(99)));
}

// Step 4 - Channel-based processing with backpressure
Console.WriteLine("\n--- Channel-Based Processing ---");

var channelConfigs = new[] { (Workers: 5, Capacity: 50), (Workers: 10, Capacity: 100) };

foreach (var (workers, capacity) in channelConfigs)
{
    var testTopic = $"{topic}-channel-{workers}-{capacity}";
    await ProduceTestMessages(testTopic, messageCount);

    var metrics = await MeasureChannelProcessing(testTopic, capacity, workers);
    Console.WriteLine($"Workers={workers}, Capacity={capacity}: {metrics.MessagesPerSecond,8:N0} msg/s, P99: {metrics.GetPercentile(99):F3}ms");
    results.Add(($"Channel ({workers} workers)", metrics.MessagesPerSecond, metrics.GetPercentile(99)));
}

// Step 5 - Batch processing
Console.WriteLine("\n--- Batch Processing ---");

var batchSizes = new[] { 10, 50, 100 };

foreach (var batchSize in batchSizes)
{
    var testTopic = $"{topic}-batch-{batchSize}";
    await ProduceTestMessages(testTopic, messageCount);

    var metrics = await MeasureBatchProcessing(testTopic, batchSize);
    Console.WriteLine($"BatchSize={batchSize,-4}: {metrics.MessagesPerSecond,8:N0} msg/s, P99: {metrics.GetPercentile(99):F3}ms");
    results.Add(($"Batch ({batchSize})", metrics.MessagesPerSecond, metrics.GetPercentile(99)));
}

// Step 6 - Strategy comparison
Console.WriteLine("\n--- Strategy Comparison ---");
Console.WriteLine($"{"Strategy",-25} {"Throughput",12} {"P99",10}");
Console.WriteLine(new string('-', 50));

foreach (var (strategy, throughput, p99) in results)
{
    Console.WriteLine($"{strategy,-25} {throughput,9:N0} msg/s {p99,7:F2}ms");
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Max useful consumers = number of partitions");
Console.WriteLine("2. Channel-based processing provides natural backpressure");
Console.WriteLine("3. Batch processing trades latency for throughput");
Console.WriteLine("4. Tune MaxPollIntervalMs to match processing time");
Console.WriteLine("5. Use SemaphoreSlim to limit concurrent processing");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Helper Methods ---

async Task<PerformanceMetrics> MeasureParallelProcessing(string testTopic, int parallelism)
{
    var metrics = new PerformanceMetrics();
    var semaphore = new SemaphoreSlim(parallelism);
    var tasks = new List<Task>();

    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.GroupId = $"parallel-group-{parallelism}";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    });
    consumer.Subscribe(testTopic);

    metrics.Start();
    var count = 0;
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    try
    {
        while (count < messageCount && !cts.Token.IsCancellationRequested)
        {
            var result = await consumer.ConsumeAsync(cts.Token);
            if (result?.Value == null) continue;

            count++;
            await semaphore.WaitAsync(cts.Token);

            var start = Stopwatch.GetTimestamp();
            var messageSize = result.Value.Length;

            var task = Task.Run(async () =>
            {
                try
                {
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
    }
    catch (OperationCanceledException) { }

    await Task.WhenAll(tasks);
    metrics.Stop();
    return metrics;
}

async Task<PerformanceMetrics> MeasureChannelProcessing(string testTopic, int channelCapacity, int workerCount)
{
    var metrics = new PerformanceMetrics();
    var channel = Channel.CreateBounded<string>(
        new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.GroupId = $"channel-group-{workerCount}-{channelCapacity}";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    });
    consumer.Subscribe(testTopic);

    metrics.Start();

    // Consumer task - reads from Surgewave and writes to channel
    var consumerTask = Task.Run(async () =>
    {
        var count = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            while (count < messageCount && !cts.Token.IsCancellationRequested)
            {
                var result = await consumer.ConsumeAsync(cts.Token);
                if (result?.Value != null)
                {
                    await channel.Writer.WriteAsync(result.Value, cts.Token);
                    count++;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            channel.Writer.Complete();
        }
    });

    // Worker tasks - read from channel and process
    var workerTasks = Enumerable.Range(0, workerCount).Select(async _ =>
    {
        await foreach (var message in channel.Reader.ReadAllAsync())
        {
            var start = Stopwatch.GetTimestamp();
            await ProcessMessageAsync(message);
            var elapsed = Stopwatch.GetElapsedTime(start);
            metrics.RecordMessage(message.Length, elapsed.TotalMilliseconds);
        }
    }).ToList();

    await consumerTask;
    await Task.WhenAll(workerTasks);
    metrics.Stop();
    return metrics;
}

async Task<PerformanceMetrics> MeasureBatchProcessing(string testTopic, int batchSize)
{
    var metrics = new PerformanceMetrics();
    var batch = new List<string>(batchSize);

    await using var consumer = new SurgewaveConsumer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.GroupId = $"batch-group-{batchSize}";
        opt.AutoOffsetReset = AutoOffsetReset.Earliest;
    });
    consumer.Subscribe(testTopic);

    metrics.Start();
    var count = 0;
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    try
    {
        while (count < messageCount && !cts.Token.IsCancellationRequested)
        {
            // Collect a batch
            while (batch.Count < batchSize && count < messageCount)
            {
                var result = await consumer.ConsumeAsync(cts.Token);
                if (result?.Value != null)
                {
                    batch.Add(result.Value);
                    count++;
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
                    metrics.RecordMessage(msg.Length, elapsed.TotalMilliseconds / batch.Count);
                }
                batch.Clear();
            }
        }
    }
    catch (OperationCanceledException) { }

    metrics.Stop();
    return metrics;
}

async Task ProcessMessageAsync(string message)
{
    // Simulate I/O-bound processing work
    await Task.Delay(1);
}

async Task ProcessBatchAsync(List<string> batch)
{
    // Simulate batch processing (e.g., bulk database insert)
    // Faster per message when batched
    await Task.Delay(Math.Max(1, batch.Count / 10));
}

// Performance Metrics class
public class PerformanceMetrics
{
    private readonly List<double> _latencies = new();
    private readonly Stopwatch _stopwatch = new();
    private long _messageCount;
    private long _byteCount;
    private readonly object _lock = new();

    public void Start() => _stopwatch.Start();
    public void Stop() => _stopwatch.Stop();

    public void RecordMessage(int messageSize, double latencyMs)
    {
        lock (_lock)
        {
            _messageCount++;
            _byteCount += messageSize;
            _latencies.Add(latencyMs);
        }
    }

    public double MessagesPerSecond =>
        _stopwatch.Elapsed.TotalSeconds > 0
            ? _messageCount / _stopwatch.Elapsed.TotalSeconds
            : 0;

    public double MegabytesPerSecond =>
        _stopwatch.Elapsed.TotalSeconds > 0
            ? _byteCount / 1024.0 / 1024.0 / _stopwatch.Elapsed.TotalSeconds
            : 0;

    public double GetPercentile(double percentile)
    {
        lock (_lock)
        {
            if (_latencies.Count == 0) return 0;
            var sorted = _latencies.OrderBy(x => x).ToList();
            var index = (int)(sorted.Count * percentile / 100.0);
            return sorted[Math.Min(index, sorted.Count - 1)];
        }
    }

    public long MessageCount => _messageCount;
    public TimeSpan Elapsed => _stopwatch.Elapsed;
}