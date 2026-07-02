// Surgewave Bootcamp - Unit 26, Lesson 1: Performance Fundamentals
//
// In this lesson, you'll learn:
// - How to measure throughput (messages/second, MB/second)
// - How to calculate latency percentiles (P50, P90, P99)
// - How to identify performance bottlenecks
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.1 - Performance Fundamentals ===");
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
var topic = "performance-test";

// Configuration
var messageCount = 10_000;
var messageSizeBytes = 1024; // 1KB messages

// TODO: Step 1 - Implement PerformanceMetrics class
// Create a class to collect throughput and latency statistics
// var metrics = new PerformanceMetrics();

// TODO: Step 2 - Create producer and measure throughput
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
// });
//
// Console.WriteLine($"\n--- Producer Performance Test ---");
// Console.WriteLine($"Producing {messageCount:N0} messages of {messageSizeBytes} bytes each...");
//
// metrics.Start();
// for (int i = 0; i < messageCount; i++)
// {
//     var start = Stopwatch.GetTimestamp();
//     var message = new string('x', messageSizeBytes);
//
//     await producer.ProduceAsync(topic, $"key-{i}", message);
//
//     var elapsed = Stopwatch.GetElapsedTime(start);
//     metrics.RecordMessage(message.Length, elapsed.TotalMilliseconds);
// }
// metrics.Stop();

// TODO: Step 3 - Print throughput statistics
// Console.WriteLine($"\nThroughput: {metrics.MessagesPerSecond:N0} msg/s");
// Console.WriteLine($"Data rate: {metrics.MegabytesPerSecond:F2} MB/s");

// TODO: Step 4 - Calculate and print latency percentiles
// Console.WriteLine("\n--- Latency Percentiles ---");
// Console.WriteLine($"P50 (median): {metrics.GetPercentile(50):F3} ms");
// Console.WriteLine($"P90: {metrics.GetPercentile(90):F3} ms");
// Console.WriteLine($"P99: {metrics.GetPercentile(99):F3} ms");
// Console.WriteLine($"P99.9: {metrics.GetPercentile(99.9):F3} ms");
// Console.WriteLine($"Max: {metrics.GetPercentile(100):F3} ms");

// TODO: Step 5 - Measure consumer performance
// var consumerMetrics = new PerformanceMetrics();
//
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
//     opt.GroupId = "perf-test-group";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
// consumer.Subscribe(topic);
//
// Console.WriteLine($"\n--- Consumer Performance Test ---");
// Console.WriteLine($"Consuming {messageCount:N0} messages...");
//
// consumerMetrics.Start();
// for (int i = 0; i < messageCount; i++)
// {
//     var start = Stopwatch.GetTimestamp();
//     var result = await consumer.ConsumeAsync();
//     if (result?.Value != null)
//     {
//         var elapsed = Stopwatch.GetElapsedTime(start);
//         consumerMetrics.RecordMessage(result.Value.Length, elapsed.TotalMilliseconds);
//     }
// }
// consumerMetrics.Stop();
//
// Console.WriteLine($"\nThroughput: {consumerMetrics.MessagesPerSecond:N0} msg/s");
// Console.WriteLine($"P99 Latency: {consumerMetrics.GetPercentile(99):F3} ms");

// TODO: Step 6 - Generate performance report
// PrintPerformanceReport(metrics, "Producer Performance");
// PrintPerformanceReport(consumerMetrics, "Consumer Performance");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use Stopwatch for high-resolution timing, not DateTime");
Console.WriteLine("2. P99 latency is critical for user experience");
Console.WriteLine("3. Throughput and latency are often trade-offs");
Console.WriteLine("4. Always warm up before measuring performance");
Console.WriteLine("5. Collect enough samples for statistical significance");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Implement PerformanceMetrics class
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
//     public double MessagesPerSecond =>
//         _stopwatch.Elapsed.TotalSeconds > 0
//             ? _messageCount / _stopwatch.Elapsed.TotalSeconds
//             : 0;
//
//     public double MegabytesPerSecond =>
//         _stopwatch.Elapsed.TotalSeconds > 0
//             ? _byteCount / 1024.0 / 1024.0 / _stopwatch.Elapsed.TotalSeconds
//             : 0;
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
//
//     public long MessageCount => _messageCount;
//     public TimeSpan Elapsed => _stopwatch.Elapsed;
// }

// TODO: Implement PrintPerformanceReport method
// static void PrintPerformanceReport(PerformanceMetrics metrics, string title)
// {
//     Console.WriteLine();
//     Console.WriteLine($"â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—");
//     Console.WriteLine($"â•‘  {title,-40} â•‘");
//     Console.WriteLine($"â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£");
//     Console.WriteLine($"â•‘  Throughput: {metrics.MessagesPerSecond,12:N0} msg/s     â•‘");
//     Console.WriteLine($"â•‘  Data Rate:  {metrics.MegabytesPerSecond,12:F2} MB/s      â•‘");
//     Console.WriteLine($"â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£");
//     Console.WriteLine($"â•‘  P50 Latency:  {metrics.GetPercentile(50),8:F3} ms          â•‘");
//     Console.WriteLine($"â•‘  P90 Latency:  {metrics.GetPercentile(90),8:F3} ms          â•‘");
//     Console.WriteLine($"â•‘  P99 Latency:  {metrics.GetPercentile(99),8:F3} ms          â•‘");
//     Console.WriteLine($"â•‘  P99.9:        {metrics.GetPercentile(99.9),8:F3} ms          â•‘");
//     Console.WriteLine($"â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");
// }
