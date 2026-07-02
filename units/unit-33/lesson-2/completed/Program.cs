// Surgewave Bootcamp - Unit 26, Lesson 2: Producer Optimization
// COMPLETED SOLUTION

using System.Diagnostics;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.2 - Producer Optimization ===");
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
var topic = "producer-optimization-test";

// Configuration
var messageCount = 10_000;
var messageSizeBytes = 1024; // 1KB messages

// Warm-up
Console.WriteLine("Warming up...");
await using (var warmupProducer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers))
{
    for (int i = 0; i < 100; i++)
        await warmupProducer.ProduceAsync("warmup-topic", "key", new string('w', 100));
}

// Step 1 - Measure baseline performance
Console.WriteLine("\n--- Baseline Performance ---");
await using (var baselineProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
}))
{
    var baselineMetrics = await MeasureProducerPerformance(baselineProducer, topic + "-baseline", messageCount, messageSizeBytes);
    Console.WriteLine($"Default settings: {baselineMetrics.MessagesPerSecond:N0} msg/s, P99: {baselineMetrics.GetPercentile(99):F2}ms");
}

// Step 2 - Test batch size variations
Console.WriteLine("\n--- Batch Size Comparison ---");
var batchSizes = new[] { 1024, 16384, 65536, 131072 };

foreach (var batchSize in batchSizes)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = batchSize;
        opt.LingerMs = 5;
    });

    var metrics = await MeasureProducerPerformance(producer, $"{topic}-batch-{batchSize}", messageCount, messageSizeBytes);
    Console.WriteLine($"BatchSize={batchSize / 1024,4}KB: {metrics.MessagesPerSecond,8:N0} msg/s, P50: {metrics.GetPercentile(50):F3}ms");
}

// Step 3 - Test linger time variations
Console.WriteLine("\n--- Linger Time Comparison ---");
var lingerTimes = new[] { 0, 1, 5, 10, 50 };

foreach (var lingerMs in lingerTimes)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = 65536;
        opt.LingerMs = lingerMs;
    });

    var metrics = await MeasureProducerPerformance(producer, $"{topic}-linger-{lingerMs}", messageCount, messageSizeBytes);
    Console.WriteLine($"LingerMs={lingerMs,3}: {metrics.MessagesPerSecond,8:N0} msg/s, P50: {metrics.GetPercentile(50):F3}ms, P99: {metrics.GetPercentile(99):F3}ms");
}

// Step 4 - Sequential vs Async comparison
Console.WriteLine("\n--- Sequential vs Async Production ---");

await using (var seqProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.BatchSize = 65536;
    opt.LingerMs = 5;
}))
{
    var seqMetrics = await MeasureProducerPerformance(seqProducer, $"{topic}-sequential", messageCount, messageSizeBytes);
    Console.WriteLine($"Sequential:  {seqMetrics.MessagesPerSecond,8:N0} msg/s");
}

// Step 5 - Async batched production with different concurrency levels
Console.WriteLine("\n--- Async Batched Production ---");
var concurrencyLevels = new[] { 10, 50, 100, 250 };

foreach (var concurrency in concurrencyLevels)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = 65536;
        opt.LingerMs = 5;
    });

    var metrics = await MeasureAsyncBatchedProduction(producer, $"{topic}-async-{concurrency}", messageCount, messageSizeBytes, concurrency);
    Console.WriteLine($"Concurrency={concurrency,3}: {metrics.MessagesPerSecond,8:N0} msg/s, P99: {metrics.GetPercentile(99):F2}ms");
}

// Step 6 - Configuration comparison
Console.WriteLine("\n--- Configuration Profiles ---");
var configurations = new[]
{
    ("Low Latency", 4096, 0),
    ("Balanced", 32768, 5),
    ("High Throughput", 131072, 20),
};

Console.WriteLine($"{"Profile",-18} {"Throughput",12} {"P50",10} {"P99",10}");
Console.WriteLine(new string('-', 54));

foreach (var (name, batchSize, lingerMs) in configurations)
{
    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = batchSize;
        opt.LingerMs = lingerMs;
    });

    var metrics = await MeasureAsyncBatchedProduction(producer, $"{topic}-profile-{name}", messageCount, messageSizeBytes, 100);
    Console.WriteLine($"{name,-18} {metrics.MessagesPerSecond,9:N0} msg/s {metrics.GetPercentile(50),7:F2}ms {metrics.GetPercentile(99),7:F2}ms");
}

// Summary
Console.WriteLine();
Console.WriteLine("â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—");
Console.WriteLine("â•‘             Producer Optimization Summary             â•‘");
Console.WriteLine("â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£");
Console.WriteLine("â•‘  1. Batch Size: Larger = more throughput, more RAM   â•‘");
Console.WriteLine("â•‘  2. LingerMs: Higher = more batching, more latency   â•‘");
Console.WriteLine("â•‘  3. Async: Concurrent sends vastly outperform sync   â•‘");
Console.WriteLine("â•‘  4. Balance: 32-64KB batch, 5-10ms linger is good    â•‘");
Console.WriteLine("â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Helper method: Measure sequential producer performance
async Task<PerformanceMetrics> MeasureProducerPerformance(
    SurgewaveProducer<string, string> producer,
    string topicName,
    int count,
    int messageSize)
{
    var metrics = new PerformanceMetrics();
    var message = new string('x', messageSize);

    metrics.Start();
    for (int i = 0; i < count; i++)
    {
        var start = Stopwatch.GetTimestamp();
        await producer.ProduceAsync(topicName, $"key-{i}", message);
        var elapsed = Stopwatch.GetElapsedTime(start);
        metrics.RecordMessage(messageSize, elapsed.TotalMilliseconds);
    }
    metrics.Stop();

    return metrics;
}

// Helper method: Measure async batched producer performance
async Task<PerformanceMetrics> MeasureAsyncBatchedProduction(
    SurgewaveProducer<string, string> producer,
    string topicName,
    int count,
    int messageSize,
    int concurrency)
{
    var metrics = new PerformanceMetrics();
    var message = new string('x', messageSize);
    var tasks = new List<Task>(concurrency);
    var lockObj = new object();

    metrics.Start();

    for (int i = 0; i < count; i++)
    {
        var start = Stopwatch.GetTimestamp();
        var task = producer.ProduceAsync(topicName, $"key-{i}", message)
            .ContinueWith(_ =>
            {
                var elapsed = Stopwatch.GetElapsedTime(start);
                lock (lockObj)
                {
                    metrics.RecordMessage(messageSize, elapsed.TotalMilliseconds);
                }
            });

        tasks.Add(task);

        if (tasks.Count >= concurrency)
        {
            await Task.WhenAll(tasks);
            tasks.Clear();
        }
    }

    if (tasks.Count > 0)
        await Task.WhenAll(tasks);

    metrics.Stop();
    return metrics;
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
