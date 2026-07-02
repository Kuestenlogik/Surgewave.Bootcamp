// Surgewave Bootcamp - Unit 26 Project: Performance Optimization Suite
// COMPLETED SOLUTION

using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Unit 26 Project - Performance Optimization Suite ===");
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
var payload = new string('x', 512);
var messageCount = 5_000;
var runner = new BenchmarkRunner(warmupIterations: 100, measureIterations: messageCount, runs: 3);
var allResults = new List<(string Category, BenchmarkResult Result, GcReport GcInfo)>();

// ========== PRODUCER BENCHMARKS ==========
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("  PRODUCER BENCHMARKS");
Console.WriteLine(new string('=', 60));

// Sequential produce
await using var seqProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

var gcProfile = new GcProfiler();
var seqResult = await runner.RunAsync("Producer: Sequential", async i =>
{
    await seqProducer.ProduceAsync("bench-seq", $"key-{i}", payload);
});
var seqGc = gcProfile.Stop();
allResults.Add(("Producer", seqResult, seqGc));
seqResult.PrintReport();

// Async batched produce
await using var asyncProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.BatchSize = 65536;
    opt.LingerMs = 5;
});

gcProfile = new GcProfiler();
var asyncResult = await runner.RunAsync("Producer: Async Batch", async i =>
{
    var tasks = new Task[10];
    for (int j = 0; j < 10; j++)
        tasks[j] = asyncProducer.ProduceAsync($"bench-async", $"key-{i}-{j}", payload);
    await Task.WhenAll(tasks);
});
var asyncGc = gcProfile.Stop();
allResults.Add(("Producer", asyncResult, asyncGc));
asyncResult.PrintReport();

// ========== CONSUMER BENCHMARKS ==========
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("  CONSUMER BENCHMARKS");
Console.WriteLine(new string('=', 60));

// Pre-populate topics
var consumerTopic = "bench-consumer";
await using var seedProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});
for (int i = 0; i < messageCount * 4; i++)
{
    await seedProducer.ProduceAsync(consumerTopic, $"key-{i}", payload);
}

// Sequential consumer
await using var seqConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "bench-seq-consumer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
seqConsumer.Subscribe(consumerTopic);

gcProfile = new GcProfiler();
var consumerSeqResult = await runner.RunAsync("Consumer: Sequential", async i =>
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await seqConsumer.ConsumeAsync(cts.Token);
});
var consumerSeqGc = gcProfile.Stop();
allResults.Add(("Consumer", consumerSeqResult, consumerSeqGc));
consumerSeqResult.PrintReport();

// ========== MEMORY BENCHMARKS ==========
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("  MEMORY BENCHMARKS");
Console.WriteLine(new string('=', 60));

// With allocations
gcProfile = new GcProfiler();
var allocResult = await runner.RunAsync("Memory: Allocations", async i =>
{
    var buffer = new byte[1024];
    for (int j = 0; j < buffer.Length; j++)
        buffer[j] = (byte)(i & 0xFF);
    var output = new byte[buffer.Length];
    Buffer.BlockCopy(buffer, 0, output, 0, buffer.Length);
    await Task.CompletedTask;
});
var allocGc = gcProfile.Stop();
allResults.Add(("Memory", allocResult, allocGc));
allocResult.PrintReport();

// With pooling
var pool = ArrayPool<byte>.Shared;
gcProfile = new GcProfiler();
var poolResult = await runner.RunAsync("Memory: Pooling", async i =>
{
    var buffer = pool.Rent(1024);
    try
    {
        var span = buffer.AsSpan(0, 1024);
        for (int j = 0; j < span.Length; j++)
            span[j] = (byte)(i & 0xFF);
        for (int j = 0; j < span.Length; j++)
            span[j] ^= 0xAA;
    }
    finally
    {
        pool.Return(buffer);
    }
    await Task.CompletedTask;
});
var poolGcReport = gcProfile.Stop();
allResults.Add(("Memory", poolResult, poolGcReport));
poolResult.PrintReport();

// Span parsing vs String.Split
var testMessages = new string[messageCount];
for (int i = 0; i < messageCount; i++)
    testMessages[i] = $"key-{i}|payload-data-{i}-with-content|2025-01-15T10:30:00Z";

gcProfile = new GcProfiler();
var splitResult = await runner.RunAsync("Parse: String.Split", async i =>
{
    var parts = testMessages[i % testMessages.Length].Split('|');
    var len = parts[0].Length + parts[1].Length + parts[2].Length;
    await Task.CompletedTask;
});
var splitGc = gcProfile.Stop();
allResults.Add(("Parse", splitResult, splitGc));

gcProfile = new GcProfiler();
var spanResult = await runner.RunAsync("Parse: Span<T>", async i =>
{
    var span = testMessages[i % testMessages.Length].AsSpan();
    var first = span.IndexOf('|');
    var last = span.LastIndexOf('|');
    var key = span[..first];
    var val = span[(first + 1)..last];
    var ts = span[(last + 1)..];
    var len = key.Length + val.Length + ts.Length;
    await Task.CompletedTask;
});
var spanGc = gcProfile.Stop();
allResults.Add(("Parse", spanResult, spanGc));

// ========== COMPARISON REPORT ==========
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("  PERFORMANCE COMPARISON REPORT");
Console.WriteLine(new string('=', 60));

Console.WriteLine($"\n{"Strategy",-28} {"Throughput",14} {"P50",9} {"P99",9} {"P99.9",9} {"GC Gen0",8}");
Console.WriteLine(new string('-', 80));

foreach (var (category, result, gc) in allResults)
{
    Console.WriteLine($"{result.Name,-28} {result.MeanThroughput,10:N0} op/s " +
        $"{result.P50,7:F4}ms {result.P99,7:F4}ms {result.P999,7:F4}ms {gc.Gen0Collections,7}");
}

// Find best in each category
Console.WriteLine("\n--- Recommendations ---");
var categories = allResults.GroupBy(r => r.Category);
foreach (var category in categories)
{
    var best = category.OrderByDescending(r => r.Result.MeanThroughput).First();
    Console.WriteLine($"Best {category.Key}: {best.Result.Name} ({best.Result.MeanThroughput:N0} op/s, P99: {best.Result.P99:F4}ms)");
}

// Latency histogram for best overall
var bestOverall = allResults.OrderByDescending(r => r.Result.MeanThroughput).First();
Console.WriteLine($"\n--- Latency Distribution: {bestOverall.Result.Name} ---");
bestOverall.Result.PrintHistogram();

Console.WriteLine();
Console.WriteLine("Project complete!");

// ========== SUPPORTING TYPES ==========

public class BenchmarkRunner
{
    private readonly int _warmupIterations;
    private readonly int _measureIterations;
    private readonly int _runs;

    public BenchmarkRunner(int warmupIterations = 100, int measureIterations = 1000, int runs = 3)
    {
        _warmupIterations = warmupIterations;
        _measureIterations = measureIterations;
        _runs = runs;
    }

    public async Task<BenchmarkResult> RunAsync(string name, Func<int, Task> action)
    {
        Console.Write($"  Running {name}...");
        for (int i = 0; i < _warmupIterations; i++)
            await action(i);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allLatencies = new List<double>();
        var runThroughputs = new List<double>();

        for (int run = 0; run < _runs; run++)
        {
            var latencies = new List<double>(_measureIterations);
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < _measureIterations; i++)
            {
                var start = Stopwatch.GetTimestamp();
                await action(i);
                var elapsed = Stopwatch.GetElapsedTime(start);
                latencies.Add(elapsed.TotalMilliseconds);
            }

            sw.Stop();
            runThroughputs.Add(_measureIterations / sw.Elapsed.TotalSeconds);
            allLatencies.AddRange(latencies);
        }

        Console.WriteLine(" done.");
        return new BenchmarkResult(name, allLatencies, runThroughputs);
    }
}

public class BenchmarkResult
{
    public string Name { get; }
    private readonly List<double> _sortedLatencies;
    private readonly List<double> _throughputs;

    public BenchmarkResult(string name, List<double> latencies, List<double> throughputs)
    {
        Name = name;
        _sortedLatencies = latencies.OrderBy(x => x).ToList();
        _throughputs = throughputs;
    }

    public double MeanThroughput => _throughputs.Average();
    public double P50 => GetPercentile(50);
    public double P90 => GetPercentile(90);
    public double P99 => GetPercentile(99);
    public double P999 => GetPercentile(99.9);

    public double GetPercentile(double p)
    {
        if (_sortedLatencies.Count == 0) return 0;
        var index = (int)(_sortedLatencies.Count * p / 100.0);
        return _sortedLatencies[Math.Min(index, _sortedLatencies.Count - 1)];
    }

    public void PrintReport()
    {
        var stdDev = _throughputs.Count < 2 ? 0 :
            Math.Sqrt(_throughputs.Sum(x => (x - _throughputs.Average()) * (x - _throughputs.Average())) / (_throughputs.Count - 1));
        Console.WriteLine($"  Throughput: {MeanThroughput:N0} +/- {stdDev:N0} op/s | P50: {P50:F4}ms | P99: {P99:F4}ms | P99.9: {P999:F4}ms");
    }

    public void PrintHistogram()
    {
        if (_sortedLatencies.Count == 0) return;
        var buckets = new (string Label, double Min, double Max)[]
        {
            ("< 0.01ms", 0, 0.01), ("0.01-0.05ms", 0.01, 0.05), ("0.05-0.1ms", 0.05, 0.1),
            ("0.1-0.5ms", 0.1, 0.5), ("0.5-1ms", 0.5, 1), ("1-5ms", 1, 5), ("> 5ms", 5, double.MaxValue)
        };

        var counts = buckets.Select(b => _sortedLatencies.Count(l => l >= b.Min && l < b.Max)).ToArray();
        var maxCount = counts.Max();

        for (int i = 0; i < buckets.Length; i++)
        {
            var pct = (double)counts[i] / _sortedLatencies.Count * 100;
            var barLen = maxCount > 0 ? (int)((double)counts[i] / maxCount * 30) : 0;
            Console.WriteLine($"  {buckets[i].Label,-14} {new string('#', barLen),-30} {counts[i],6} ({pct:F1}%)");
        }
    }
}

public class GcProfiler
{
    private readonly int _gen0Start = GC.CollectionCount(0);
    private readonly int _gen1Start = GC.CollectionCount(1);
    private readonly int _gen2Start = GC.CollectionCount(2);
    private readonly long _allocStart = GC.GetTotalAllocatedBytes(precise: true);

    public GcReport Stop() => new(
        GC.CollectionCount(0) - _gen0Start,
        GC.CollectionCount(1) - _gen1Start,
        GC.CollectionCount(2) - _gen2Start,
        GC.GetTotalAllocatedBytes(precise: true) - _allocStart
    );
}

public record GcReport(int Gen0Collections, int Gen1Collections, int Gen2Collections, long AllocatedBytes);