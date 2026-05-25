# Lesson 9.5: Performance Profiling - Instructions

## Step 1: Create a Performance Profiler

Build a profiler to collect timing data:

```csharp
public class PerformanceProfiler
{
    private readonly List<double> _latencies = new();
    private readonly Stopwatch _totalTime = new();
    private int _operationCount;

    public void Start() => _totalTime.Start();

    public void RecordOperation(TimeSpan latency)
    {
        _latencies.Add(latency.TotalMilliseconds);
        Interlocked.Increment(ref _operationCount);
    }

    public void Stop() => _totalTime.Stop();

    public double ThroughputPerSecond =>
        _operationCount / _totalTime.Elapsed.TotalSeconds;
}
```

## Step 2: Measure Producer Throughput

Profile message production:

```csharp
var profiler = new PerformanceProfiler();
profiler.Start();

for (var i = 0; i < 10000; i++)
{
    var start = Stopwatch.GetTimestamp();
    await producer.ProduceAsync(topic, key, value);
    var elapsed = Stopwatch.GetElapsedTime(start);
    profiler.RecordOperation(elapsed);
}

profiler.Stop();
Console.WriteLine($"Throughput: {profiler.ThroughputPerSecond:F2} msgs/sec");
```

## Step 3: Calculate Latency Percentiles

Compute p50, p90, p99 latencies:

```csharp
public LatencyStats GetLatencyStats()
{
    var sorted = _latencies.OrderBy(x => x).ToList();
    return new LatencyStats
    {
        Min = sorted.First(),
        Max = sorted.Last(),
        Avg = sorted.Average(),
        P50 = sorted[(int)(sorted.Count * 0.50)],
        P90 = sorted[(int)(sorted.Count * 0.90)],
        P99 = sorted[(int)(sorted.Count * 0.99)]
    };
}
```

## Step 4: Profile Memory Usage

Monitor allocations and GC:

```csharp
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
        return new MemoryStats
        {
            AllocatedBytes = GC.GetTotalMemory(false) - _startMemory,
            Gen0Collections = GC.CollectionCount(0) - _startGen0,
            Gen1Collections = GC.CollectionCount(1) - _startGen1,
            Gen2Collections = GC.CollectionCount(2) - _startGen2
        };
    }
}
```

## Step 5: Identify Bottlenecks

Profile different parts of your code:

```csharp
// Profile serialization
using (profiler.MeasureSection("serialization"))
{
    var json = JsonSerializer.Serialize(message);
}

// Profile network call
using (profiler.MeasureSection("produce"))
{
    await producer.ProduceAsync(topic, key, value);
}

// Profile deserialization
using (profiler.MeasureSection("deserialization"))
{
    var message = JsonSerializer.Deserialize<Message>(json);
}
```

## Step 6: Generate a Performance Report

Create a summary report:

```csharp
public void PrintReport()
{
    Console.WriteLine("=== Performance Report ===");
    Console.WriteLine($"Total Operations: {_operationCount}");
    Console.WriteLine($"Total Time: {_totalTime.Elapsed}");
    Console.WriteLine($"Throughput: {ThroughputPerSecond:F2} ops/sec");
    Console.WriteLine();
    Console.WriteLine("Latency (ms):");
    var stats = GetLatencyStats();
    Console.WriteLine($"  Min: {stats.Min:F3}");
    Console.WriteLine($"  P50: {stats.P50:F3}");
    Console.WriteLine($"  P90: {stats.P90:F3}");
    Console.WriteLine($"  P99: {stats.P99:F3}");
    Console.WriteLine($"  Max: {stats.Max:F3}");
}
```

## Verification

Run the completed solution:
```bash
cd completed
dotnet run
```

You should see:
- Throughput measurements for produce/consume
- Latency percentiles (p50, p90, p99)
- Memory allocation and GC statistics
