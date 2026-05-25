// Surgewave Bootcamp - Unit 26, Lesson 3: Consumer Optimization
//
// In this lesson, you'll learn:
// - How to tune fetch size and poll intervals
// - Parallel and channel-based consumption patterns
// - Partition assignment strategies for scaling
// - Batch processing for throughput optimization
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Threading.Channels;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.3 - Consumer Optimization ===");
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

// TODO: Step 2 - Measure baseline consumer performance (sequential)
// Create a consumer with default settings, consume all messages,
// and measure throughput and latency.
//
// var baselineMetrics = new PerformanceMetrics();
// await using var baselineConsumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
//     opt.GroupId = "baseline-group";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
// baselineConsumer.Subscribe(topic);
//
// baselineMetrics.Start();
// var consumed = 0;
// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
// while (consumed < messageCount && !cts.Token.IsCancellationRequested)
// {
//     var start = Stopwatch.GetTimestamp();
//     var result = await baselineConsumer.ConsumeAsync(cts.Token);
//     if (result?.Value != null)
//     {
//         var elapsed = Stopwatch.GetElapsedTime(start);
//         baselineMetrics.RecordMessage(result.Value.Length, elapsed.TotalMilliseconds);
//         consumed++;
//     }
// }
// baselineMetrics.Stop();
// Console.WriteLine($"\nBaseline: {baselineMetrics.MessagesPerSecond:N0} msg/s, P99: {baselineMetrics.GetPercentile(99):F3}ms");

// TODO: Step 3 - Implement parallel processing with SemaphoreSlim
// Use a semaphore to limit concurrent processing of messages
// while consuming from a single consumer instance.
//
// async Task<PerformanceMetrics> MeasureParallelProcessing(int parallelism)
// {
//     // Create a new topic for each test run to avoid offset conflicts
//     var testTopic = $"{topic}-parallel-{parallelism}";
//     // Re-produce messages to the test topic...
//     // Create consumer, consume with parallelism...
// }

// TODO: Step 4 - Implement channel-based processing with backpressure
// Use System.Threading.Channels to decouple consumption from processing.
//
// async Task<PerformanceMetrics> MeasureChannelProcessing(int channelCapacity, int workerCount)
// {
//     var channel = Channel.CreateBounded<string>(
//         new BoundedChannelOptions(channelCapacity)
//         {
//             FullMode = BoundedChannelFullMode.Wait
//         });
//     // Producer task reads from consumer, writes to channel
//     // Worker tasks read from channel and process
// }

// TODO: Step 5 - Implement batch processing
// Collect messages into batches before processing for efficiency.
//
// async Task<PerformanceMetrics> MeasureBatchProcessing(int batchSize)
// {
//     // Collect batchSize messages, then process them all at once
// }

// TODO: Step 6 - Compare all strategies
// Console.WriteLine("\n--- Strategy Comparison ---");
// Console.WriteLine($"{"Strategy",-25} {"Throughput",12} {"P99",10}");
// Console.WriteLine(new string('-', 50));
//
// var results = new List<(string Strategy, double Throughput, double P99)>();
// // Run each strategy and add to results...

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Max useful consumers = number of partitions");
Console.WriteLine("2. Channel-based processing provides natural backpressure");
Console.WriteLine("3. Batch processing trades latency for throughput");
Console.WriteLine("4. Tune MaxPollIntervalMs to match processing time");
Console.WriteLine("5. Use SemaphoreSlim to limit concurrent processing");

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

// TODO: Implement ProcessMessageAsync helper
// async Task ProcessMessageAsync(string message)
// {
//     // Simulate I/O-bound processing work
//     await Task.Delay(1);
// }