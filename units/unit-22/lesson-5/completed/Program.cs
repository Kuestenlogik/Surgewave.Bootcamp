// Surgewave Bootcamp - Unit 9, Lesson 5: Performance Profiling
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.5 - Performance Profiling ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Demo 1: Producer Throughput ==============
Console.WriteLine("--- Demo 1: Producer Throughput ---");

var produceProfiler = new PerformanceProfiler("Producer");
var memoryProfiler = new MemoryProfiler();

await using var producer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

memoryProfiler.Start();
produceProfiler.Start();

const int messageCount = 500;
var payload = Encoding.UTF8.GetBytes(new string('x', 100)); // 100 byte payload

for (var i = 0; i < messageCount; i++)
{
    var key = Encoding.UTF8.GetBytes($"key-{i}");
    var start = Stopwatch.GetTimestamp();
    await producer.ProduceAsync("perf-test", key, payload);
    var elapsed = Stopwatch.GetElapsedTime(start);
    produceProfiler.RecordOperation(elapsed);
}

produceProfiler.Stop();
var produceMemory = memoryProfiler.Stop();

Console.WriteLine($"  Messages: {messageCount}");
Console.WriteLine($"  Throughput: {produceProfiler.ThroughputPerSecond:F2} msgs/sec");
Console.WriteLine($"  Total time: {produceProfiler.TotalTime.TotalMilliseconds:F2}ms");
Console.WriteLine();

// ============== Demo 2: Latency Percentiles ==============
Console.WriteLine("--- Demo 2: Latency Percentiles ---");

var latencyStats = produceProfiler.GetLatencyStats();

Console.WriteLine("  Producer Latency:");
Console.WriteLine($"    Min:  {latencyStats.Min:F3}ms");
Console.WriteLine($"    P50:  {latencyStats.P50:F3}ms");
Console.WriteLine($"    P90:  {latencyStats.P90:F3}ms");
Console.WriteLine($"    P99:  {latencyStats.P99:F3}ms");
Console.WriteLine($"    Max:  {latencyStats.Max:F3}ms");
Console.WriteLine($"    Avg:  {latencyStats.Avg:F3}ms");
Console.WriteLine();

// ============== Demo 3: Consumer Throughput ==============
Console.WriteLine("--- Demo 3: Consumer Throughput ---");

var consumeProfiler = new PerformanceProfiler("Consumer");

await using var consumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "perf-test-group";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("perf-test");

memoryProfiler.Start();
consumeProfiler.Start();

var consumed = 0;
var consumeTimeout = DateTime.UtcNow.AddSeconds(5);

while (consumed < messageCount && DateTime.UtcNow < consumeTimeout)
{
    var start = Stopwatch.GetTimestamp();
    var result = await consumer.ConsumeAsync(TimeSpan.FromMilliseconds(100));
    var elapsed = Stopwatch.GetElapsedTime(start);

    if (result != null)
    {
        consumeProfiler.RecordOperation(elapsed);
        consumed++;
    }
}

consumeProfiler.Stop();
var consumeMemory = memoryProfiler.Stop();

Console.WriteLine($"  Messages: {consumed}");
Console.WriteLine($"  Throughput: {consumeProfiler.ThroughputPerSecond:F2} msgs/sec");
Console.WriteLine($"  Total time: {consumeProfiler.TotalTime.TotalMilliseconds:F2}ms");
Console.WriteLine();

var consumeLatency = consumeProfiler.GetLatencyStats();
Console.WriteLine("  Consumer Latency:");
Console.WriteLine($"    Min:  {consumeLatency.Min:F3}ms");
Console.WriteLine($"    P50:  {consumeLatency.P50:F3}ms");
Console.WriteLine($"    P90:  {consumeLatency.P90:F3}ms");
Console.WriteLine($"    P99:  {consumeLatency.P99:F3}ms");
Console.WriteLine($"    Max:  {consumeLatency.Max:F3}ms");
Console.WriteLine();

// ============== Demo 4: Memory Profiling ==============
Console.WriteLine("--- Demo 4: Memory Profiling ---");

Console.WriteLine("  Producer Memory:");
Console.WriteLine($"    Allocated: {FormatBytes(produceMemory.AllocatedBytes)}");
Console.WriteLine($"    Per message: {FormatBytes(produceMemory.AllocatedBytes / messageCount)}");
Console.WriteLine($"    GC Gen0: {produceMemory.Gen0Collections}");
Console.WriteLine($"    GC Gen1: {produceMemory.Gen1Collections}");
Console.WriteLine($"    GC Gen2: {produceMemory.Gen2Collections}");
Console.WriteLine();

Console.WriteLine("  Consumer Memory:");
Console.WriteLine($"    Allocated: {FormatBytes(consumeMemory.AllocatedBytes)}");
Console.WriteLine($"    Per message: {FormatBytes(consumeMemory.AllocatedBytes / Math.Max(1, consumed))}");
Console.WriteLine($"    GC Gen0: {consumeMemory.Gen0Collections}");
Console.WriteLine($"    GC Gen1: {consumeMemory.Gen1Collections}");
Console.WriteLine($"    GC Gen2: {consumeMemory.Gen2Collections}");
Console.WriteLine();

// ============== Demo 5: Section Profiling ==============
Console.WriteLine("--- Demo 5: Section Profiling ---");

var sectionProfiler = new SectionProfiler();

for (var i = 0; i < 100; i++)
{
    // Profile serialization
    object message;
    using (sectionProfiler.MeasureSection("serialize"))
    {
        message = new { id = i, data = $"message-{i}", timestamp = DateTime.UtcNow };
        _ = JsonSerializer.Serialize(message);
    }

    // Profile produce
    using (sectionProfiler.MeasureSection("produce"))
    {
        await producer.ProduceAsync("section-test",
            Encoding.UTF8.GetBytes($"key-{i}"),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));
    }
}

Console.WriteLine("  Section timing breakdown:");
sectionProfiler.PrintReport();
Console.WriteLine();

// ============== Demo 6: Bottleneck Identification ==============
Console.WriteLine("--- Demo 6: Bottleneck Identification ---");

var bottleneckAnalyzer = new BottleneckAnalyzer();
bottleneckAnalyzer.AddProfile("Producer", produceProfiler);
bottleneckAnalyzer.AddProfile("Consumer", consumeProfiler);
bottleneckAnalyzer.AddSections(sectionProfiler);

bottleneckAnalyzer.Analyze();
Console.WriteLine();

// ============== Demo 7: Performance Report ==============
Console.WriteLine("--- Demo 7: Performance Report ---");
Console.WriteLine();

Console.WriteLine("  ╔════════════════════════════════════════════════════════╗");
Console.WriteLine("  ║           PERFORMANCE SUMMARY REPORT                   ║");
Console.WriteLine("  ╠════════════════════════════════════════════════════════╣");
Console.WriteLine($"  ║  Producer Throughput:  {produceProfiler.ThroughputPerSecond,10:F2} msgs/sec       ║");
Console.WriteLine($"  ║  Consumer Throughput:  {consumeProfiler.ThroughputPerSecond,10:F2} msgs/sec       ║");
Console.WriteLine($"  ║  Producer P99 Latency: {latencyStats.P99,10:F3} ms             ║");
Console.WriteLine($"  ║  Consumer P99 Latency: {consumeLatency.P99,10:F3} ms             ║");
Console.WriteLine($"  ║  Total GC Collections: {produceMemory.Gen0Collections + consumeMemory.Gen0Collections,10}                  ║");
Console.WriteLine("  ╚════════════════════════════════════════════════════════╝");

Console.WriteLine();

// Summary
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Measure throughput in msgs/sec for capacity planning");
Console.WriteLine("2. Use P99 latency (not average) for SLA targets");
Console.WriteLine("3. Monitor allocations to reduce GC pressure");
Console.WriteLine("4. Profile sections to identify bottlenecks");
Console.WriteLine("5. Compare before/after when optimizing");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

static string FormatBytes(long bytes)
{
    if (bytes < 1024) return $"{bytes} B";
    if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
    return $"{bytes / (1024.0 * 1024):F2} MB";
}

// ============== Helper Classes ==============

public record LatencyStats(double Min, double Max, double Avg, double P50, double P90, double P99);

public record MemoryStats(long AllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections);

public class PerformanceProfiler
{
    private readonly string _name;
    private readonly List<double> _latencies = new();
    private readonly Stopwatch _totalTime = new();
    private int _operationCount;

    public PerformanceProfiler(string name) => _name = name;

    public string Name => _name;
    public TimeSpan TotalTime => _totalTime.Elapsed;
    public int OperationCount => _operationCount;
    public double ThroughputPerSecond => _operationCount / _totalTime.Elapsed.TotalSeconds;

    public void Start() => _totalTime.Start();
    public void Stop() => _totalTime.Stop();

    public void RecordOperation(TimeSpan latency)
    {
        lock (_latencies)
        {
            _latencies.Add(latency.TotalMilliseconds);
        }
        Interlocked.Increment(ref _operationCount);
    }

    public LatencyStats GetLatencyStats()
    {
        List<double> sorted;
        lock (_latencies)
        {
            sorted = _latencies.OrderBy(x => x).ToList();
        }

        if (sorted.Count == 0)
        {
            return new LatencyStats(0, 0, 0, 0, 0, 0);
        }

        return new LatencyStats(
            Min: sorted.First(),
            Max: sorted.Last(),
            Avg: sorted.Average(),
            P50: sorted[(int)(sorted.Count * 0.50)],
            P90: sorted[(int)(sorted.Count * 0.90)],
            P99: sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.99))]
        );
    }
}

public class MemoryProfiler
{
    private long _startMemory;
    private int _startGen0;
    private int _startGen1;
    private int _startGen2;

    public void Start()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _startMemory = GC.GetTotalMemory(true);
        _startGen0 = GC.CollectionCount(0);
        _startGen1 = GC.CollectionCount(1);
        _startGen2 = GC.CollectionCount(2);
    }

    public MemoryStats Stop()
    {
        var endMemory = GC.GetTotalMemory(false);
        return new MemoryStats(
            AllocatedBytes: Math.Max(0, endMemory - _startMemory),
            Gen0Collections: GC.CollectionCount(0) - _startGen0,
            Gen1Collections: GC.CollectionCount(1) - _startGen1,
            Gen2Collections: GC.CollectionCount(2) - _startGen2
        );
    }
}

public class SectionProfiler
{
    private readonly Dictionary<string, List<double>> _sections = new();
    private readonly object _lock = new();

    public IDisposable MeasureSection(string name)
    {
        return new SectionMeasurement(this, name);
    }

    internal void RecordSection(string name, double milliseconds)
    {
        lock (_lock)
        {
            if (!_sections.TryGetValue(name, out var list))
            {
                list = new List<double>();
                _sections[name] = list;
            }
            list.Add(milliseconds);
        }
    }

    public Dictionary<string, (double Avg, double P99, double Total)> GetSectionStats()
    {
        var result = new Dictionary<string, (double, double, double)>();
        lock (_lock)
        {
            foreach (var (name, times) in _sections)
            {
                var sorted = times.OrderBy(x => x).ToList();
                var avg = sorted.Average();
                var p99 = sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.99))];
                var total = sorted.Sum();
                result[name] = (avg, p99, total);
            }
        }
        return result;
    }

    public void PrintReport()
    {
        var stats = GetSectionStats();
        var total = stats.Values.Sum(s => s.Total);

        foreach (var (name, (avg, p99, sectionTotal)) in stats.OrderByDescending(x => x.Value.Total))
        {
            var pct = total > 0 ? (sectionTotal / total * 100) : 0;
            Console.WriteLine($"    {name,-15} Avg: {avg:F3}ms, P99: {p99:F3}ms, Total: {sectionTotal:F1}ms ({pct:F1}%)");
        }
    }

    private class SectionMeasurement : IDisposable
    {
        private readonly SectionProfiler _profiler;
        private readonly string _name;
        private readonly long _start;

        public SectionMeasurement(SectionProfiler profiler, string name)
        {
            _profiler = profiler;
            _name = name;
            _start = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_start);
            _profiler.RecordSection(_name, elapsed.TotalMilliseconds);
        }
    }
}

public class BottleneckAnalyzer
{
    private readonly Dictionary<string, PerformanceProfiler> _profiles = new();
    private SectionProfiler? _sections;

    public void AddProfile(string name, PerformanceProfiler profiler)
    {
        _profiles[name] = profiler;
    }

    public void AddSections(SectionProfiler sections)
    {
        _sections = sections;
    }

    public void Analyze()
    {
        Console.WriteLine("  Bottleneck Analysis:");
        Console.WriteLine();

        // Analyze throughput
        var minThroughput = _profiles.MinBy(p => p.Value.ThroughputPerSecond);
        if (minThroughput.Value != null)
        {
            Console.WriteLine($"    Throughput bottleneck: {minThroughput.Key}");
            Console.WriteLine($"      - Current: {minThroughput.Value.ThroughputPerSecond:F2} msgs/sec");
            Console.WriteLine($"      - Recommendation: Optimize or scale this component");
        }

        // Analyze latency
        var maxP99 = _profiles.MaxBy(p => p.Value.GetLatencyStats().P99);
        if (maxP99.Value != null)
        {
            var stats = maxP99.Value.GetLatencyStats();
            Console.WriteLine($"    Latency bottleneck: {maxP99.Key}");
            Console.WriteLine($"      - P99: {stats.P99:F3}ms");
            Console.WriteLine($"      - Recommendation: Investigate high latency operations");
        }

        // Analyze sections
        if (_sections != null)
        {
            var sectionStats = _sections.GetSectionStats();
            var maxSection = sectionStats.MaxBy(s => s.Value.Total);
            Console.WriteLine($"    Code section bottleneck: {maxSection.Key}");
            Console.WriteLine($"      - Total time: {maxSection.Value.Total:F1}ms");
            Console.WriteLine($"      - Recommendation: Optimize this section first");
        }
    }
}
