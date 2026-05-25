# Knowledge Assessment: Lesson 26.5 - Benchmarking & Profiling

## Multiple Choice

### 1. Why is a warm-up phase essential before collecting benchmark measurements?
- [ ] A) It reduces network latency
- [ ] B) JIT compilation on first runs distorts timing results
- [ ] C) It fills the message queue
- [ ] D) It configures the GC

<details>
<summary>Answer</summary>
**B)** The .NET JIT compiler compiles methods to native code on first invocation. These first executions are significantly slower due to compilation overhead. A warm-up phase ensures all code paths are JIT-compiled and in CPU cache before measurements begin, giving accurate steady-state results.
</details>

### 2. What does a high standard deviation in benchmark throughput indicate?
- [ ] A) The benchmark is fast
- [ ] B) Results are inconsistent, possibly due to external factors
- [ ] C) The code is well-optimized
- [ ] D) More warm-up iterations are needed

<details>
<summary>Answer</summary>
**B)** High standard deviation means throughput varies significantly across runs. Common causes include: GC pauses, OS scheduling interference, thermal throttling, background processes, or contention. The benchmark should be re-run in a more controlled environment or with more iterations.
</details>

### 3. At 100,000 requests/second, how many users per second experience P99 latency or worse?
- [ ] A) 100
- [ ] B) 1,000
- [ ] C) 10,000
- [ ] D) 99,000

<details>
<summary>Answer</summary>
**B)** P99 means 99% of requests are faster. At 100K req/s, 1% = 1,000 requests per second experience latency at P99 or worse. This is why P99 matters even though it sounds like "almost everyone is fast."
</details>

### 4. Which tool is best suited for identifying GC-related latency spikes?
- [ ] A) BenchmarkDotNet
- [ ] B) PerfView with GC events
- [ ] C) dotnet-format
- [ ] D) Visual Studio debugger

<details>
<summary>Answer</summary>
**B)** PerfView collects ETW events including detailed GC information (collection durations, generations, pause times, allocation rates). It can show exactly when GC pauses occurred and correlate them with latency spikes. BenchmarkDotNet can measure allocations but doesn't provide real-time GC event correlation.
</details>

### 5. When comparing two optimization strategies, which metric is most important for real-time applications?
- [ ] A) Mean throughput
- [ ] B) Maximum throughput
- [ ] C) P99 latency
- [ ] D) Total bytes allocated

<details>
<summary>Answer</summary>
**C)** For real-time applications, consistent low latency matters more than peak throughput. P99 latency shows the worst-case experience for 99% of operations. A strategy with slightly lower throughput but much better P99 is usually preferred for real-time workloads.
</details>

## True/False

### 6. Running benchmarks in Debug mode gives accurate performance measurements.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Debug mode disables optimizations, includes debug symbols, and may include additional checks (bounds checking, assertions). Always benchmark in Release mode to get production-representative results. BenchmarkDotNet enforces this by default.
</details>

### 7. BenchmarkDotNet automatically handles warm-up, iteration count, and statistical analysis.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - BenchmarkDotNet automatically determines the appropriate number of warm-up and measurement iterations based on the benchmark's characteristics. It calculates mean, median, standard deviation, and confidence intervals, and formats results in a standard table.
</details>

### 8. Measuring latency of a single request is sufficient to understand performance.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - A single measurement is meaningless due to variance from JIT, GC, OS scheduling, and other factors. You need many measurements to build a latency distribution. Only percentiles (P50, P90, P99) computed over many samples give meaningful performance insights.
</details>

## Code Challenge

### 9. What problems exist in this benchmarking code?

```csharp
var start = DateTime.Now;

for (int i = 0; i < 100; i++)
{
    await producer.ProduceAsync(topic, "key", "value");
}

var elapsed = DateTime.Now - start;
var throughput = 100 / elapsed.TotalSeconds;
Console.WriteLine($"Throughput: {throughput} msg/s");
```

<details>
<summary>Answer</summary>
**Issues:**

1. **DateTime.Now is low resolution** - Only ~15ms accuracy on Windows. Use `Stopwatch.GetTimestamp()` for high-resolution timing.
2. **No warm-up phase** - First iterations include JIT compilation time.
3. **Only 100 iterations** - Too few for statistical significance.
4. **No percentile measurement** - Only measures total time, not individual latency distribution.
5. **No multiple runs** - Single run doesn't account for variance.
6. **No GC tracking** - GC pauses are invisible.
7. **Hardcoded key/value** - May not represent realistic workload.

**Corrected implementation:**

```csharp
// Warm-up
for (int i = 0; i < 200; i++)
    await producer.ProduceAsync(topic, $"warmup-{i}", "value");

GC.Collect();
GC.WaitForPendingFinalizers();

var latencies = new List<double>(10000);
var gcBefore = GC.CollectionCount(0);

var totalSw = Stopwatch.StartNew();

for (int i = 0; i < 10000; i++)
{
    var start = Stopwatch.GetTimestamp();
    await producer.ProduceAsync(topic, $"key-{i}", new string('x', 512));
    var elapsed = Stopwatch.GetElapsedTime(start);
    latencies.Add(elapsed.TotalMilliseconds);
}

totalSw.Stop();

var gcAfter = GC.CollectionCount(0);
var sorted = latencies.OrderBy(x => x).ToList();

Console.WriteLine($"Throughput: {10000 / totalSw.Elapsed.TotalSeconds:N0} msg/s");
Console.WriteLine($"P50: {sorted[(int)(sorted.Count * 0.50)]:F4}ms");
Console.WriteLine($"P99: {sorted[(int)(sorted.Count * 0.99)]:F4}ms");
Console.WriteLine($"P99.9: {sorted[(int)(sorted.Count * 0.999)]:F4}ms");
Console.WriteLine($"GC Gen0 collections: {gcAfter - gcBefore}");
```
</details>

### 10. Design a benchmark suite to validate that an optimization achieves its goals.

**Scenario**: You've implemented ArrayPool-based buffer management for Surgewave message processing. Design a benchmark to prove:
1. Throughput improved by at least 30%
2. P99 latency improved by at least 20%
3. Memory allocations reduced by at least 80%

<details>
<summary>Answer</summary>
**Benchmark Suite Design:**

```csharp
public class OptimizationBenchmarkSuite
{
    private readonly BenchmarkRunner _runner;
    private readonly int _messageCount = 10_000;
    private readonly int _runs = 5;

    public async Task<BenchmarkVerification> VerifyOptimization()
    {
        // Run baseline (without optimization)
        var baselineGcBefore = GC.GetTotalAllocatedBytes(precise: true);
        var baseline = await _runner.RunAsync("Baseline", async i =>
        {
            var buffer = new byte[1024]; // Allocates each time
            FillBuffer(buffer, i);
            ProcessBuffer(buffer);
        });
        var baselineAllocations = GC.GetTotalAllocatedBytes(precise: true) - baselineGcBefore;

        // Force GC between tests
        GC.Collect(2, GCCollectionMode.Forced, true);
        await Task.Delay(100);

        // Run optimized (with ArrayPool)
        var pool = ArrayPool<byte>.Shared;
        var optimizedGcBefore = GC.GetTotalAllocatedBytes(precise: true);
        var optimized = await _runner.RunAsync("Optimized", async i =>
        {
            var buffer = pool.Rent(1024);
            try
            {
                FillBuffer(buffer, i);
                ProcessBufferInPlace(buffer.AsSpan(0, 1024));
            }
            finally
            {
                pool.Return(buffer);
            }
        });
        var optimizedAllocations = GC.GetTotalAllocatedBytes(precise: true) - optimizedGcBefore;

        // Verify assertions
        var throughputImprovement =
            (optimized.MeanThroughput / baseline.MeanThroughput - 1) * 100;
        var p99Improvement =
            (1 - optimized.P99 / baseline.P99) * 100;
        var allocationReduction =
            (1 - (double)optimizedAllocations / baselineAllocations) * 100;

        return new BenchmarkVerification(
            ThroughputImproved: throughputImprovement >= 30,
            ThroughputDelta: throughputImprovement,
            P99Improved: p99Improvement >= 20,
            P99Delta: p99Improvement,
            AllocationsReduced: allocationReduction >= 80,
            AllocationDelta: allocationReduction,
            Baseline: baseline,
            Optimized: optimized
        );
    }
}

public record BenchmarkVerification(
    bool ThroughputImproved, double ThroughputDelta,
    bool P99Improved, double P99Delta,
    bool AllocationsReduced, double AllocationDelta,
    BenchmarkResult Baseline, BenchmarkResult Optimized
)
{
    public bool AllGoalsMet =>
        ThroughputImproved && P99Improved && AllocationsReduced;

    public void PrintReport()
    {
        var pass = AllGoalsMet ? "PASS" : "FAIL";
        Console.WriteLine($"\n=== Optimization Verification: {pass} ===");
        Console.WriteLine($"Throughput: {(ThroughputImproved ? "PASS" : "FAIL")} " +
            $"({ThroughputDelta:+0.0;-0.0}%, goal: +30%)");
        Console.WriteLine($"P99 Latency: {(P99Improved ? "PASS" : "FAIL")} " +
            $"({P99Delta:+0.0;-0.0}%, goal: -20%)");
        Console.WriteLine($"Allocations: {(AllocationsReduced ? "PASS" : "FAIL")} " +
            $"({AllocationDelta:+0.0;-0.0}%, goal: -80%)");
    }
}
```

**Key design principles:**
1. **Multiple runs** (5) for statistical significance
2. **GC between tests** to isolate measurements
3. **Precise allocation tracking** with `GC.GetTotalAllocatedBytes(precise: true)`
4. **Pass/fail assertions** for each optimization goal
5. **Comprehensive report** showing exact improvements
</details>

## Score
- 10/10: Excellent! Ready for the unit project
- 8-9/10: Good understanding, review missed concepts
- 5-7/10: Review the lesson before continuing
- <5/10: Re-do the lesson and exercises
