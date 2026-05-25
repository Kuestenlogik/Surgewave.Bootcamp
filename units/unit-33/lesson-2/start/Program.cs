// Surgewave Bootcamp - Unit 26, Lesson 2: Producer Optimization
//
// In this lesson, you'll learn:
// - How to tune batch size and linger time
// - Compression algorithm trade-offs
// - High-throughput async production patterns
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.2 - Producer Optimization ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var bootstrapServers = runtime.BootstrapServers;
var topic = "producer-optimization-test";

// Configuration
var messageCount = 10_000;
var messageSizeBytes = 1024; // 1KB messages

// TODO: Step 1 - Measure baseline performance
// Console.WriteLine("\n--- Baseline Performance ---");
// await using var baselineProducer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
// });
//
// var baselineMetrics = await MeasureProducerPerformance(baselineProducer, topic + "-baseline", messageCount, messageSizeBytes);
// Console.WriteLine($"Default settings: {baselineMetrics.MessagesPerSecond:N0} msg/s, P99: {baselineMetrics.GetPercentile(99):F2}ms");

// TODO: Step 2 - Test batch size variations
// Console.WriteLine("\n--- Batch Size Comparison ---");
// var batchSizes = new[] { 1024, 16384, 65536, 262144 };
//
// foreach (var batchSize in batchSizes)
// {
//     await using var producer = new SurgewaveProducer<string, string>(opt =>
//     {
//         opt.BootstrapServers = bootstrapServers;
//         opt.BatchSize = batchSize;
//         opt.LingerMs = 5;
//     });
//
//     var metrics = await MeasureProducerPerformance(producer, $"{topic}-batch-{batchSize}", messageCount, messageSizeBytes);
//     Console.WriteLine($"BatchSize={batchSize / 1024}KB: {metrics.MessagesPerSecond:N0} msg/s");
// }

// TODO: Step 3 - Test linger time variations
// Console.WriteLine("\n--- Linger Time Comparison ---");
// var lingerTimes = new[] { 0, 1, 5, 10, 50 };
//
// foreach (var lingerMs in lingerTimes)
// {
//     await using var producer = new SurgewaveProducer<string, string>(opt =>
//     {
//         opt.BootstrapServers = bootstrapServers;
//         opt.BatchSize = 65536;
//         opt.LingerMs = lingerMs;
//     });
//
//     var metrics = await MeasureProducerPerformance(producer, $"{topic}-linger-{lingerMs}", messageCount, messageSizeBytes);
//     Console.WriteLine($"LingerMs={lingerMs}: {metrics.MessagesPerSecond:N0} msg/s, P50: {metrics.GetPercentile(50):F2}ms");
// }

// TODO: Step 4 - Test compression algorithms
// Console.WriteLine("\n--- Compression Comparison ---");
// Compare None, LZ4, Snappy, Zstd, Gzip

// TODO: Step 5 - Implement async batched production
// Console.WriteLine("\n--- Async Batched Production ---");
// var concurrencyLevels = new[] { 10, 50, 100, 500 };
//
// foreach (var concurrency in concurrencyLevels)
// {
//     await using var producer = new SurgewaveProducer<string, string>(opt =>
//     {
//         opt.BootstrapServers = bootstrapServers;
//         opt.BatchSize = 65536;
//         opt.LingerMs = 5;
//     });
//
//     var metrics = await MeasureAsyncBatchedProduction(producer, $"{topic}-async-{concurrency}", messageCount, messageSizeBytes, concurrency);
//     Console.WriteLine($"Concurrency={concurrency}: {metrics.MessagesPerSecond:N0} msg/s");
// }

// TODO: Step 6 - Find optimal configuration
// Console.WriteLine("\n--- Configuration Comparison ---");
// Test combinations and find best settings for your workload

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Larger batches improve throughput but add latency");
Console.WriteLine("2. LingerMs allows batches to fill, trading latency for throughput");
Console.WriteLine("3. LZ4 is the best default compression choice");
Console.WriteLine("4. Async batched production can dramatically improve throughput");
Console.WriteLine("5. Choose configuration based on your latency requirements");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Implement MeasureProducerPerformance
// async Task<PerformanceMetrics> MeasureProducerPerformance(
//     SurgewaveProducer<string, string> producer,
//     string topic,
//     int messageCount,
//     int messageSize)
// {
//     var metrics = new PerformanceMetrics();
//     var message = new string('x', messageSize);
//
//     metrics.Start();
//     for (int i = 0; i < messageCount; i++)
//     {
//         var start = Stopwatch.GetTimestamp();
//         await producer.ProduceAsync(topic, $"key-{i}", message);
//         var elapsed = Stopwatch.GetElapsedTime(start);
//         metrics.RecordMessage(messageSize, elapsed.TotalMilliseconds);
//     }
//     metrics.Stop();
//
//     return metrics;
// }

// TODO: Implement MeasureAsyncBatchedProduction
// async Task<PerformanceMetrics> MeasureAsyncBatchedProduction(
//     SurgewaveProducer<string, string> producer,
//     string topic,
//     int messageCount,
//     int messageSize,
//     int concurrency)
// {
//     var metrics = new PerformanceMetrics();
//     var message = new string('x', messageSize);
//     var tasks = new List<Task>(concurrency);
//
//     metrics.Start();
//     for (int i = 0; i < messageCount; i++)
//     {
//         var start = Stopwatch.GetTimestamp();
//         var task = producer.ProduceAsync(topic, $"key-{i}", message)
//             .ContinueWith(_ =>
//             {
//                 var elapsed = Stopwatch.GetElapsedTime(start);
//                 metrics.RecordMessage(messageSize, elapsed.TotalMilliseconds);
//             });
//
//         tasks.Add(task);
//
//         if (tasks.Count >= concurrency)
//         {
//             await Task.WhenAll(tasks);
//             tasks.Clear();
//         }
//     }
//
//     if (tasks.Count > 0)
//         await Task.WhenAll(tasks);
//
//     metrics.Stop();
//     return metrics;
// }

// TODO: PerformanceMetrics class (same as Lesson 26.1)
// public class PerformanceMetrics
// {
//     private readonly List<double> _latencies = new();
//     private readonly Stopwatch _stopwatch = new();
//     private long _messageCount;
//     private long _byteCount;
//     private readonly object _lock = new();
//
//     public void Start() => _stopwatch.Start();
//     public void Stop() => _stopwatch.Stop();
//
//     public void RecordMessage(int messageSize, double latencyMs)
//     {
//         lock (_lock)
//         {
//             _messageCount++;
//             _byteCount += messageSize;
//             _latencies.Add(latencyMs);
//         }
//     }
//
//     public double MessagesPerSecond => _messageCount / _stopwatch.Elapsed.TotalSeconds;
//     public double MegabytesPerSecond => _byteCount / 1024.0 / 1024.0 / _stopwatch.Elapsed.TotalSeconds;
//
//     public double GetPercentile(double percentile)
//     {
//         lock (_lock)
//         {
//             if (_latencies.Count == 0) return 0;
//             var sorted = _latencies.OrderBy(x => x).ToList();
//             var index = (int)(sorted.Count * percentile / 100.0);
//             return sorted[Math.Min(index, sorted.Count - 1)];
//         }
//     }
// }
