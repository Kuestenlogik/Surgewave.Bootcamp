# Lesson 26.1: Instructions

Follow these step-by-step instructions to implement performance measurement fundamentals.

## Step 1: Create a Performance Metrics Collector

Implement a class to collect and calculate performance statistics:

```csharp
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
        _messageCount / _stopwatch.Elapsed.TotalSeconds;

    public double MegabytesPerSecond =>
        _byteCount / 1024.0 / 1024.0 / _stopwatch.Elapsed.TotalSeconds;

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
}
```

## Step 2: Implement High-Resolution Timing

Use `Stopwatch` for accurate latency measurement:

```csharp
// High-resolution timing pattern
var startTimestamp = Stopwatch.GetTimestamp();

// ... perform operation ...

var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
var elapsedMs = (double)elapsedTicks / Stopwatch.Frequency * 1000.0;
```

Or using .NET 7+ helper:

```csharp
var start = Stopwatch.GetTimestamp();
await operation();
var elapsed = Stopwatch.GetElapsedTime(start);
var latencyMs = elapsed.TotalMilliseconds;
```

## Step 3: Measure Producer Throughput

Measure messages per second during production:

```csharp
var metrics = new PerformanceMetrics();
var messageCount = 10_000;
var messageSize = 1024; // 1KB messages

await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = bootstrapServers);

metrics.Start();

for (int i = 0; i < messageCount; i++)
{
    var start = Stopwatch.GetTimestamp();
    var message = new string('x', messageSize);

    await producer.ProduceAsync(topic, $"key-{i}", message);

    var elapsed = Stopwatch.GetElapsedTime(start);
    metrics.RecordMessage(message.Length, elapsed.TotalMilliseconds);
}

metrics.Stop();

Console.WriteLine($"Throughput: {metrics.MessagesPerSecond:N0} msg/s");
Console.WriteLine($"Data rate: {metrics.MegabytesPerSecond:F2} MB/s");
```

## Step 4: Calculate Latency Percentiles

Compute P50, P90, P99, and P99.9:

```csharp
Console.WriteLine("\n--- Latency Percentiles ---");
Console.WriteLine($"P50 (median): {metrics.GetPercentile(50):F2} ms");
Console.WriteLine($"P90: {metrics.GetPercentile(90):F2} ms");
Console.WriteLine($"P95: {metrics.GetPercentile(95):F2} ms");
Console.WriteLine($"P99: {metrics.GetPercentile(99):F2} ms");
Console.WriteLine($"P99.9: {metrics.GetPercentile(99.9):F2} ms");
Console.WriteLine($"Max: {metrics.GetPercentile(100):F2} ms");
```

## Step 5: Measure End-to-End Latency

Track time from production to consumption:

```csharp
// Include timestamp in message
var produceTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
var payload = $"{produceTime}|{messageContent}";

await producer.ProduceAsync(topic, key, payload);

// On consumer side
var result = await consumer.ConsumeAsync();
var parts = result.Value.Split('|', 2);
var sentTime = long.Parse(parts[0]);
var receivedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
var e2eLatencyMs = receivedTime - sentTime;
```

## Step 6: Compare Different Configurations

Test performance with varying settings:

```csharp
var configurations = new[]
{
    ("Small Batch", 1024, 0),     // 1KB batch, no linger
    ("Medium Batch", 16384, 1),   // 16KB batch, 1ms linger
    ("Large Batch", 65536, 10),   // 64KB batch, 10ms linger
};

foreach (var (name, batchSize, lingerMs) in configurations)
{
    Console.WriteLine($"\n--- Testing: {name} ---");

    await using var producer = new SurgewaveProducer<string, string>(opt =>
    {
        opt.BootstrapServers = bootstrapServers;
        opt.BatchSize = batchSize;
        opt.LingerMs = lingerMs;
    });

    var metrics = MeasureProducerPerformance(producer, messageCount);
    PrintMetrics(name, metrics);
}
```

## Step 7: Generate Performance Report

Create a summary of all measurements:

```csharp
void PrintPerformanceReport(PerformanceMetrics metrics, string title)
{
    Console.WriteLine();
    Console.WriteLine($"╔══════════════════════════════════════════╗");
    Console.WriteLine($"║  {title,-40} ║");
    Console.WriteLine($"╠══════════════════════════════════════════╣");
    Console.WriteLine($"║  Throughput: {metrics.MessagesPerSecond,12:N0} msg/s     ║");
    Console.WriteLine($"║  Data Rate:  {metrics.MegabytesPerSecond,12:F2} MB/s      ║");
    Console.WriteLine($"╠══════════════════════════════════════════╣");
    Console.WriteLine($"║  P50 Latency:  {metrics.GetPercentile(50),8:F2} ms          ║");
    Console.WriteLine($"║  P90 Latency:  {metrics.GetPercentile(90),8:F2} ms          ║");
    Console.WriteLine($"║  P99 Latency:  {metrics.GetPercentile(99),8:F2} ms          ║");
    Console.WriteLine($"║  P99.9 Latency:{metrics.GetPercentile(99.9),8:F2} ms          ║");
    Console.WriteLine($"╚══════════════════════════════════════════╝");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Performance metrics collection
- Throughput measurement
- Latency percentile calculation
- Configuration comparison
- Performance reporting

## Sample Output

```
=== Surgewave Bootcamp: Lesson 26.1 - Performance Fundamentals ===

Surgewave broker started on port 12345

--- Baseline Performance Test ---
Producing 10,000 messages of 1KB each...

╔══════════════════════════════════════════╗
║  Baseline Producer Performance           ║
╠══════════════════════════════════════════╣
║  Throughput:       45,230 msg/s          ║
║  Data Rate:          44.17 MB/s          ║
╠══════════════════════════════════════════╣
║  P50 Latency:      0.02 ms               ║
║  P90 Latency:      0.04 ms               ║
║  P99 Latency:      0.12 ms               ║
║  P99.9 Latency:    0.85 ms               ║
╚══════════════════════════════════════════╝

--- Consumer Performance Test ---
Consuming 10,000 messages...

╔══════════════════════════════════════════╗
║  Consumer Performance                    ║
╠══════════════════════════════════════════╣
║  Throughput:       62,500 msg/s          ║
║  Data Rate:          61.04 MB/s          ║
╠══════════════════════════════════════════╣
║  P50 Latency:      0.01 ms               ║
║  P99 Latency:      0.08 ms               ║
╚══════════════════════════════════════════╝

--- End-to-End Latency Test ---
Measuring production to consumption time...

E2E P50: 0.15 ms
E2E P99: 1.25 ms

Lesson complete!
```

## Knowledge Check

1. What is the difference between P50 and P99 latency?
2. Why might high throughput come at the cost of increased latency?
3. How does batch size affect producer throughput?
4. What causes tail latency (P99.9) spikes?
5. When should you optimize for throughput vs latency?

---

**Next:** [Lesson 26.2: Producer Optimization](../lesson-2/README.md)
