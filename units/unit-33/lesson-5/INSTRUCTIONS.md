# Lesson 26.5: Instructions

Follow these step-by-step instructions to implement benchmarking and profiling techniques.

## Step 1: Build a Benchmarking Harness

Create a reusable benchmark runner:

```csharp
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
        for (int i = 0; i < _warmupIterations; i++)
            await action(i);

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allLatencies = new List<double>();
        var runResults = new List<double>();

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
            runResults.Add(throughput);
            allLatencies.AddRange(latencies);
        }

        return new BenchmarkResult(name, allLatencies, runResults);
    }
}
```

## Step 2: Implement BenchmarkResult with Statistics

Calculate percentiles and statistical measures:

```csharp
public class BenchmarkResult
{
    public string Name { get; }
    private readonly List<double> _latencies;
    private readonly List<double> _throughputs;

    public BenchmarkResult(string name, List<double> latencies, List<double> throughputs)
    {
        Name = name;
        _latencies = latencies.OrderBy(x => x).ToList();
        _throughputs = throughputs;
    }

    public double MeanThroughput => _throughputs.Average();
    public double StdDevThroughput => CalculateStdDev(_throughputs);
    public double P50 => GetPercentile(50);
    public double P90 => GetPercentile(90);
    public double P95 => GetPercentile(95);
    public double P99 => GetPercentile(99);
    public double P999 => GetPercentile(99.9);
    public double Min => _latencies.First();
    public double Max => _latencies.Last();

    public double GetPercentile(double p)
    {
        var index = (int)(_latencies.Count * p / 100.0);
        return _latencies[Math.Min(index, _latencies.Count - 1)];
    }

    private static double CalculateStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        return Math.Sqrt(values.Sum(x => (x - avg) * (x - avg)) / (values.Count - 1));
    }

    public void PrintReport()
    {
        Console.WriteLine($"\n--- {Name} ---");
        Console.WriteLine($"Throughput: {MeanThroughput:N0} +/- {StdDevThroughput:N0} ops/s");
        Console.WriteLine($"Latency (ms):");
        Console.WriteLine($"  Min:   {Min:F4}");
        Console.WriteLine($"  P50:   {P50:F4}");
        Console.WriteLine($"  P90:   {P90:F4}");
        Console.WriteLine($"  P95:   {P95:F4}");
        Console.WriteLine($"  P99:   {P99:F4}");
        Console.WriteLine($"  P99.9: {P999:F4}");
        Console.WriteLine($"  Max:   {Max:F4}");
    }
}
```

## Step 3: Benchmark Surgewave Producer Operations

Compare different producer configurations:

```csharp
var runner = new BenchmarkRunner(warmupIterations: 200, measureIterations: 5000, runs: 3);

// Benchmark: Sequential produce
var seqResult = await runner.RunAsync("Sequential Produce", async i =>
{
    await producer.ProduceAsync(topic, $"key-{i}", payload);
});
seqResult.PrintReport();

// Benchmark: Fire-and-forget
var fireResult = await runner.RunAsync("Fire-and-Forget Produce", async i =>
{
    _ = producer.ProduceAsync(topic, $"key-{i}", payload);
    // Note: In real apps, you'd track these tasks
});
fireResult.PrintReport();
```

## Step 4: Profile GC During Workloads

Track GC metrics alongside benchmark:

```csharp
public class GcProfiler : IDisposable
{
    private readonly GcSnapshot _startSnapshot;
    private readonly long _startAllocatedBytes;
    private readonly Stopwatch _stopwatch;

    public GcProfiler()
    {
        _startSnapshot = GcSnapshot.Capture();
        _startAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true);
        _stopwatch = Stopwatch.StartNew();
    }

    public GcReport Dispose()
    {
        _stopwatch.Stop();
        var endSnapshot = GcSnapshot.Capture();
        var endAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true);

        return new GcReport(
            Duration: _stopwatch.Elapsed,
            Gen0Collections: endSnapshot.Gen0 - _startSnapshot.Gen0,
            Gen1Collections: endSnapshot.Gen1 - _startSnapshot.Gen1,
            Gen2Collections: endSnapshot.Gen2 - _startSnapshot.Gen2,
            TotalAllocatedBytes: endAllocatedBytes - _startAllocatedBytes
        );
    }
}
```

## Step 5: Compare Optimization Strategies

Run all benchmarks and generate a comparison report:

```csharp
Console.WriteLine("\n--- Performance Comparison ---");
Console.WriteLine($"{"Strategy",-25} {"Throughput",12} {"P50",8} {"P99",8} {"P99.9",8} {"Alloc",10}");
Console.WriteLine(new string('-', 75));

foreach (var result in allResults)
{
    Console.WriteLine($"{result.Name,-25} {result.MeanThroughput,9:N0}/s " +
        $"{result.P50,6:F3}ms {result.P99,6:F3}ms {result.P999,6:F3}ms");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Benchmarking harness with warm-up and multiple runs
- Statistical analysis with standard deviation
- Latency percentile calculation
- GC profiling during benchmarks
- Strategy comparison report

## Sample Output

```
=== Surgewave Bootcamp: Lesson 26.5 - Benchmarking & Profiling ===

Surgewave broker started on port 12345

--- Warm-Up Phase ---
Warming up (200 iterations)... done.

--- Sequential Produce ---
Throughput: 25,400 +/- 1,200 ops/s
Latency (ms):
  Min:   0.0120
  P50:   0.0350
  P90:   0.0480
  P95:   0.0550
  P99:   0.1200
  P99.9: 0.4500
  Max:   1.2300

--- Async Batched Produce ---
Throughput: 89,200 +/- 3,100 ops/s
Latency (ms):
  Min:   0.0080
  P50:   0.0250
  P90:   0.0380
  P95:   0.0420
  P99:   0.0850
  P99.9: 0.3200
  Max:   0.9800

--- Consumer Sequential ---
Throughput: 32,100 +/- 1,800 ops/s
Latency (ms):
  P50:   0.0280
  P99:   0.0950
  P99.9: 0.3800

--- GC Profile ---
                Gen0  Gen1  Gen2  Allocated
Sequential:       12     2     0    45.2 MB
Async Batch:       5     1     0    22.1 MB
With Pooling:      1     0     0     2.3 MB

--- Performance Comparison ---
Strategy                   Throughput      P50      P99    P99.9      Alloc
---------------------------------------------------------------------------
Sequential Produce          25,400/s  0.035ms  0.120ms  0.450ms    45.2 MB
Async Batched Produce       89,200/s  0.025ms  0.085ms  0.320ms    22.1 MB
Consumer Sequential         32,100/s  0.028ms  0.095ms  0.380ms    38.7 MB
Consumer Channel (10)       78,400/s  0.018ms  0.065ms  0.280ms    15.4 MB

Lesson complete!
```

## Knowledge Check

1. Why is warm-up important before benchmarking?
2. What is the difference between P99 and P99.9, and when does each matter?
3. How do you ensure benchmark results are statistically significant?
4. What does a high standard deviation in throughput measurements indicate?
5. How do GC collections affect tail latency percentiles?

---

**Next:** [Unit 26 Project: Performance Optimization](../project/README.md)
