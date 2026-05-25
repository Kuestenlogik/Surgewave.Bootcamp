// Surgewave Bootcamp - Unit 26, Lesson 1: Performance Fundamentals
// COMPLETED SOLUTION

using System.Diagnostics;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.1 - Performance Fundamentals ===");
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
var topic = "performance-test";

// Configuration
var messageCount = 10_000;
var messageSizeBytes = 1024; // 1KB messages

// Step 1 - Create performance metrics collector
var producerMetrics = new PerformanceMetrics();

// Step 2 - Create producer and measure throughput
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

Console.WriteLine($"\n--- Producer Performance Test ---");
Console.WriteLine($"Producing {messageCount:N0} messages of {messageSizeBytes} bytes each...");

// Warm-up phase (important for accurate measurements)
Console.WriteLine("Warming up...");
for (int i = 0; i < 100; i++)
{
    await producer.ProduceAsync(topic, "warmup", new string('w', 100));
}

// Reset topic for actual test
var testTopic = "performance-test-actual";

producerMetrics.Start();
for (int i = 0; i < messageCount; i++)
{
    var start = Stopwatch.GetTimestamp();
    var message = new string('x', messageSizeBytes);

    await producer.ProduceAsync(testTopic, $"key-{i}", message);

    var elapsed = Stopwatch.GetElapsedTime(start);
    producerMetrics.RecordMessage(message.Length, elapsed.TotalMilliseconds);
}
producerMetrics.Stop();

// Step 3 - Print throughput statistics
Console.WriteLine($"\nThroughput: {producerMetrics.MessagesPerSecond:N0} msg/s");
Console.WriteLine($"Data rate: {producerMetrics.MegabytesPerSecond:F2} MB/s");
Console.WriteLine($"Total time: {producerMetrics.Elapsed.TotalSeconds:F2} seconds");

// Step 4 - Calculate and print latency percentiles
Console.WriteLine("\n--- Producer Latency Percentiles ---");
Console.WriteLine($"P50 (median): {producerMetrics.GetPercentile(50):F3} ms");
Console.WriteLine($"P90: {producerMetrics.GetPercentile(90):F3} ms");
Console.WriteLine($"P95: {producerMetrics.GetPercentile(95):F3} ms");
Console.WriteLine($"P99: {producerMetrics.GetPercentile(99):F3} ms");
Console.WriteLine($"P99.9: {producerMetrics.GetPercentile(99.9):F3} ms");
Console.WriteLine($"Max: {producerMetrics.GetPercentile(100):F3} ms");

// Step 5 - Measure consumer performance
var consumerMetrics = new PerformanceMetrics();

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "perf-test-group";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
consumer.Subscribe(testTopic);

Console.WriteLine($"\n--- Consumer Performance Test ---");
Console.WriteLine($"Consuming {messageCount:N0} messages...");

consumerMetrics.Start();
var consumed = 0;
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    while (consumed < messageCount && !cts.Token.IsCancellationRequested)
    {
        var start = Stopwatch.GetTimestamp();
        var result = await consumer.ConsumeAsync(cts.Token);

        if (result?.Value != null)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            consumerMetrics.RecordMessage(result.Value.Length, elapsed.TotalMilliseconds);
            consumed++;
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine($"Timeout reached after consuming {consumed} messages");
}

consumerMetrics.Stop();

Console.WriteLine($"\nThroughput: {consumerMetrics.MessagesPerSecond:N0} msg/s");
Console.WriteLine($"Data rate: {consumerMetrics.MegabytesPerSecond:F2} MB/s");

Console.WriteLine("\n--- Consumer Latency Percentiles ---");
Console.WriteLine($"P50: {consumerMetrics.GetPercentile(50):F3} ms");
Console.WriteLine($"P99: {consumerMetrics.GetPercentile(99):F3} ms");
Console.WriteLine($"P99.9: {consumerMetrics.GetPercentile(99.9):F3} ms");

// Step 6 - Generate performance report
PrintPerformanceReport(producerMetrics, "Producer Performance");
PrintPerformanceReport(consumerMetrics, "Consumer Performance");

// Latency distribution analysis
Console.WriteLine("\n--- Latency Distribution Analysis ---");
AnalyzeLatencyDistribution(producerMetrics);

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use Stopwatch.GetTimestamp() for high-resolution timing");
Console.WriteLine("2. P99 latency is critical for user experience");
Console.WriteLine("3. Throughput and latency are often trade-offs");
Console.WriteLine("4. Always warm up before measuring performance");
Console.WriteLine("5. Collect enough samples for statistical significance");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

static void PrintPerformanceReport(PerformanceMetrics metrics, string title)
{
    var hl = new string('\u2550', 43); // horizontal line
    Console.WriteLine();
    Console.WriteLine($"\u2554{hl}\u2557");
    Console.WriteLine($"\u2551  {title,-40} \u2551");
    Console.WriteLine($"\u2560{hl}\u2563");
    Console.WriteLine($"\u2551  Messages:     {metrics.MessageCount,12:N0}            \u2551");
    Console.WriteLine($"\u2551  Duration:     {metrics.Elapsed.TotalSeconds,12:F2} sec        \u2551");
    Console.WriteLine($"\u2551  Throughput:   {metrics.MessagesPerSecond,12:N0} msg/s     \u2551");
    Console.WriteLine($"\u2551  Data Rate:    {metrics.MegabytesPerSecond,12:F2} MB/s      \u2551");
    Console.WriteLine($"\u2560{hl}\u2563");
    Console.WriteLine($"\u2551  Avg Latency:  {metrics.Average,12:F3} ms        \u2551");
    Console.WriteLine($"\u2551  P50 Latency:  {metrics.GetPercentile(50),12:F3} ms        \u2551");
    Console.WriteLine($"\u2551  P90 Latency:  {metrics.GetPercentile(90),12:F3} ms        \u2551");
    Console.WriteLine($"\u2551  P99 Latency:  {metrics.GetPercentile(99),12:F3} ms        \u2551");
    Console.WriteLine($"\u2551  P99.9:        {metrics.GetPercentile(99.9),12:F3} ms        \u2551");
    Console.WriteLine($"\u2551  Max Latency:  {metrics.GetPercentile(100),12:F3} ms        \u2551");
    Console.WriteLine($"\u255a{hl}\u255d");
}

static void AnalyzeLatencyDistribution(PerformanceMetrics metrics)
{
    var latencies = metrics.GetLatencies();
    if (latencies.Count == 0) return;

    // Create histogram buckets
    var buckets = new Dictionary<string, int>
    {
        { "< 0.1ms", 0 },
        { "0.1-0.5ms", 0 },
        { "0.5-1ms", 0 },
        { "1-5ms", 0 },
        { "5-10ms", 0 },
        { "> 10ms", 0 }
    };

    foreach (var latency in latencies)
    {
        if (latency < 0.1) buckets["< 0.1ms"]++;
        else if (latency < 0.5) buckets["0.1-0.5ms"]++;
        else if (latency < 1) buckets["0.5-1ms"]++;
        else if (latency < 5) buckets["1-5ms"]++;
        else if (latency < 10) buckets["5-10ms"]++;
        else buckets["> 10ms"]++;
    }

    var maxCount = buckets.Values.Max();
    var barWidth = 30;

    foreach (var (range, count) in buckets)
    {
        var percentage = (double)count / latencies.Count * 100;
        var barLength = (int)((double)count / maxCount * barWidth);
        var bar = new string('\u2588', barLength);
        Console.WriteLine($"  {range,-12} {bar,-30} {count,6} ({percentage:F1}%)");
    }
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

    public double Average
    {
        get
        {
            lock (_lock)
            {
                return _latencies.Count > 0 ? _latencies.Average() : 0;
            }
        }
    }

    public double StandardDeviation
    {
        get
        {
            lock (_lock)
            {
                if (_latencies.Count < 2) return 0;
                var avg = _latencies.Average();
                var sumSquares = _latencies.Sum(x => (x - avg) * (x - avg));
                return Math.Sqrt(sumSquares / (_latencies.Count - 1));
            }
        }
    }

    public IReadOnlyList<double> GetLatencies()
    {
        lock (_lock)
        {
            return _latencies.ToList();
        }
    }

    public long MessageCount => _messageCount;
    public long ByteCount => _byteCount;
    public TimeSpan Elapsed => _stopwatch.Elapsed;
}
