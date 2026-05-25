// Surgewave Bootcamp - Unit 26, Lesson 5: Benchmarking & Profiling
// COMPLETED SOLUTION

using System.Buffers;
using System.Diagnostics;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.5 - Benchmarking & Profiling ===");
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
var topic = "benchmark-test";
var payload = new string('x', 512);

// Step 1 - Create benchmark runner
var runner = new BenchmarkRunner(warmupIterations: 200, measureIterations: 5000, runs: 3);

// Step 2 - Benchmark sequential producer
Console.WriteLine("\n--- Benchmarking Producer Operations ---");

await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

var seqResult = await runner.RunAsync("Sequential Produce", async i =>
{
    await producer.ProduceAsync($"{topic}-seq", $"key-{i}", payload);
});
seqResult.PrintReport();

// Step 3 - Benchmark async batched producer
var asyncResult = await runner.RunAsync("Async Batched Produce", async i =>
{
    // Fire multiple produces concurrently
    var tasks = new List<Task>(10);
    for (int j = 0; j < 10; j++)
    {
        tasks.Add(producer.ProduceAsync($"{topic}-async", $"key-{i}-{j}", payload));
    }
    await Task.WhenAll(tasks);
});
asyncResult.PrintReport();

// Step 4 - Benchmark consumer
Console.WriteLine("\n--- Benchmarking Consumer Operations ---");

// Pre-populate topic
var consumerTopic = $"{topic}-consumer";
for (int i = 0; i < 10_000; i++)
{
    await producer.ProduceAsync(consumerTopic, $"key-{i}", payload);
}

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "benchmark-group";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
consumer.Subscribe(consumerTopic);

var consumerResult = await runner.RunAsync("Consumer Sequential", async i =>
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var result = await consumer.ConsumeAsync(cts.Token);
});
consumerResult.PrintReport();

// Step 5 - GC profiling comparison
Console.WriteLine("\n--- GC Profile: Allocation vs Pooling ---");

// With allocations
var gcAllocBefore = GcSnapshot.Capture();
var allocBytesBefore = GC.GetTotalAllocatedBytes(precise: true);

var allocResult = await runner.RunAsync("With Allocations", async i =>
{
    var buffer = new byte[1024];
    for (int j = 0; j < buffer.Length; j++)
        buffer[j] = (byte)(i & 0xFF);
    var output = new byte[buffer.Length];
    Buffer.BlockCopy(buffer, 0, output, 0, buffer.Length);
    await Task.CompletedTask;
});

var gcAllocAfter = GcSnapshot.Capture();
var allocBytesAfter = GC.GetTotalAllocatedBytes(precise: true);

// With pooling
GC.Collect();
GC.WaitForPendingFinalizers();

var pool = ArrayPool<byte>.Shared;
var gcPoolBefore = GcSnapshot.Capture();
var poolBytesBefore = GC.GetTotalAllocatedBytes(precise: true);

var poolResult = await runner.RunAsync("With Pooling", async i =>
{
    var buffer = pool.Rent(1024);
    try
    {
        for (int j = 0; j < 1024; j++)
            buffer[j] = (byte)(i & 0xFF);
        // Process in place, no copy needed
        var span = buffer.AsSpan(0, 1024);
        for (int j = 0; j < span.Length; j++)
            span[j] ^= 0xAA;
    }
    finally
    {
        pool.Return(buffer);
    }
    await Task.CompletedTask;
});

var gcPoolAfter = GcSnapshot.Capture();
var poolBytesAfter = GC.GetTotalAllocatedBytes(precise: true);

Console.WriteLine($"\n{"Strategy",-20} {"Gen0",6} {"Gen1",6} {"Gen2",6} {"Allocated",12}");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"{"Allocations",-20} {gcAllocAfter.Gen0 - gcAllocBefore.Gen0,6} " +
    $"{gcAllocAfter.Gen1 - gcAllocBefore.Gen1,6} {gcAllocAfter.Gen2 - gcAllocBefore.Gen2,6} " +
    $"{(allocBytesAfter - allocBytesBefore) / 1024.0 / 1024.0,9:F1} MB");
Console.WriteLine($"{"Pooling",-20} {gcPoolAfter.Gen0 - gcPoolBefore.Gen0,6} " +
    $"{gcPoolAfter.Gen1 - gcPoolBefore.Gen1,6} {gcPoolAfter.Gen2 - gcPoolBefore.Gen2,6} " +
    $"{(poolBytesAfter - poolBytesBefore) / 1024.0 / 1024.0,9:F1} MB");

// Step 6 - Comprehensive comparison
Console.WriteLine("\n--- Performance Comparison ---");
Console.WriteLine($"{"Strategy",-25} {"Throughput",14} {"P50",9} {"P99",9} {"P99.9",9}");
Console.WriteLine(new string('-', 70));

var allResults = new[] { seqResult, asyncResult, consumerResult, allocResult, poolResult };
foreach (var result in allResults)
{
    Console.WriteLine($"{result.Name,-25} {result.MeanThroughput,10:N0} op/s " +
        $"{result.P50,7:F4}ms {result.P99,7:F4}ms {result.P999,7:F4}ms");
}

// Latency distribution histogram for sequential producer
Console.WriteLine("\n--- Latency Distribution (Sequential Produce) ---");
seqResult.PrintHistogram();

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Always warm up before measuring (JIT compilation)");
Console.WriteLine("2. Use Stopwatch.GetTimestamp() for high-resolution timing");
Console.WriteLine("3. Run multiple iterations for statistical significance");
Console.WriteLine("4. Report percentiles (P50, P99), not just averages");
Console.WriteLine("5. Track GC collections alongside latency measurements");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Supporting Types ---

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
        // Warm-up phase
        Console.Write($"  Benchmarking {name}...");
        for (int i = 0; i < _warmupIterations; i++)
            await action(i);

        // Force GC before measurement
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
            var throughput = _measureIterations / sw.Elapsed.TotalSeconds;
            runThroughputs.Add(throughput);
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

    public double StdDevThroughput
    {
        get
        {
            if (_throughputs.Count < 2) return 0;
            var avg = _throughputs.Average();
            return Math.Sqrt(_throughputs.Sum(x => (x - avg) * (x - avg)) / (_throughputs.Count - 1));
        }
    }

    public double P50 => GetPercentile(50);
    public double P90 => GetPercentile(90);
    public double P95 => GetPercentile(95);
    public double P99 => GetPercentile(99);
    public double P999 => GetPercentile(99.9);
    public double Min => _sortedLatencies.FirstOrDefault();
    public double Max => _sortedLatencies.LastOrDefault();
    public double Mean => _sortedLatencies.Count > 0 ? _sortedLatencies.Average() : 0;

    public double GetPercentile(double p)
    {
        if (_sortedLatencies.Count == 0) return 0;
        var index = (int)(_sortedLatencies.Count * p / 100.0);
        return _sortedLatencies[Math.Min(index, _sortedLatencies.Count - 1)];
    }

    public void PrintReport()
    {
        Console.WriteLine($"\n  --- {Name} ---");
        Console.WriteLine($"  Throughput: {MeanThroughput:N0} +/- {StdDevThroughput:N0} ops/s");
        Console.WriteLine($"  Latency (ms):");
        Console.WriteLine($"    Min:   {Min:F4}");
        Console.WriteLine($"    P50:   {P50:F4}");
        Console.WriteLine($"    P90:   {P90:F4}");
        Console.WriteLine($"    P95:   {P95:F4}");
        Console.WriteLine($"    P99:   {P99:F4}");
        Console.WriteLine($"    P99.9: {P999:F4}");
        Console.WriteLine($"    Max:   {Max:F4}");
    }

    public void PrintHistogram()
    {
        if (_sortedLatencies.Count == 0) return;

        var buckets = new Dictionary<string, int>
        {
            { "< 0.01ms", 0 },
            { "0.01-0.05ms", 0 },
            { "0.05-0.1ms", 0 },
            { "0.1-0.5ms", 0 },
            { "0.5-1ms", 0 },
            { "1-5ms", 0 },
            { "> 5ms", 0 }
        };

        foreach (var latency in _sortedLatencies)
        {
            if (latency < 0.01) buckets["< 0.01ms"]++;
            else if (latency < 0.05) buckets["0.01-0.05ms"]++;
            else if (latency < 0.1) buckets["0.05-0.1ms"]++;
            else if (latency < 0.5) buckets["0.1-0.5ms"]++;
            else if (latency < 1) buckets["0.5-1ms"]++;
            else if (latency < 5) buckets["1-5ms"]++;
            else buckets["> 5ms"]++;
        }

        var maxCount = buckets.Values.Max();
        var barWidth = 30;

        foreach (var (range, count) in buckets)
        {
            var percentage = (double)count / _sortedLatencies.Count * 100;
            var barLength = maxCount > 0 ? (int)((double)count / maxCount * barWidth) : 0;
            var bar = new string('#', barLength);
            Console.WriteLine($"  {range,-14} {bar,-30} {count,6} ({percentage:F1}%)");
        }
    }
}

public record GcSnapshot(int Gen0, int Gen1, int Gen2, long TotalMemory)
{
    public static GcSnapshot Capture() =>
        new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetTotalMemory(false));
}