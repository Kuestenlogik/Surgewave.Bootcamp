# Lesson 26.4: Instructions

Follow these step-by-step instructions to implement memory management and GC tuning techniques.

## Step 1: Compare Allocation-Heavy vs Pooled Buffer Approaches

First, measure the impact of repeated allocations:

```csharp
// Allocation-heavy approach
async Task<(PerformanceMetrics Metrics, long Allocations)> MeasureWithAllocations(int messageCount)
{
    var metrics = new PerformanceMetrics();
    var initialBytes = GC.GetTotalAllocatedBytes(precise: true);

    metrics.Start();
    for (int i = 0; i < messageCount; i++)
    {
        var start = Stopwatch.GetTimestamp();

        // Each iteration allocates a new byte array
        var buffer = new byte[1024];
        FillBuffer(buffer, i);
        var processed = ProcessBuffer(buffer); // Allocates result

        var elapsed = Stopwatch.GetElapsedTime(start);
        metrics.RecordMessage(buffer.Length, elapsed.TotalMilliseconds);
    }
    metrics.Stop();

    var totalAllocated = GC.GetTotalAllocatedBytes(precise: true) - initialBytes;
    return (metrics, totalAllocated);
}
```

Then, measure with ArrayPool:

```csharp
// Pooled approach
async Task<(PerformanceMetrics Metrics, long Allocations)> MeasureWithPooling(int messageCount)
{
    var metrics = new PerformanceMetrics();
    var pool = ArrayPool<byte>.Shared;
    var initialBytes = GC.GetTotalAllocatedBytes(precise: true);

    metrics.Start();
    for (int i = 0; i < messageCount; i++)
    {
        var start = Stopwatch.GetTimestamp();

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

        var elapsed = Stopwatch.GetElapsedTime(start);
        metrics.RecordMessage(1024, elapsed.TotalMilliseconds);
    }
    metrics.Stop();

    var totalAllocated = GC.GetTotalAllocatedBytes(precise: true) - initialBytes;
    return (metrics, totalAllocated);
}
```

## Step 2: Use Span<T> for Zero-Copy Message Parsing

Parse structured messages without allocating substrings:

```csharp
// Zero-copy message parsing
static (ReadOnlySpan<char> Key, ReadOnlySpan<char> Value, ReadOnlySpan<char> Timestamp)
    ParseMessageZeroCopy(ReadOnlySpan<char> message)
{
    var firstSep = message.IndexOf('|');
    var lastSep = message.LastIndexOf('|');

    var key = message[..firstSep];
    var value = message[(firstSep + 1)..lastSep];
    var timestamp = message[(lastSep + 1)..];

    return (key, value, timestamp);
}

// Compared to allocation-heavy approach:
static (string Key, string Value, string Timestamp) ParseMessageWithAllocations(string message)
{
    var parts = message.Split('|'); // Allocates string[]
    return (parts[0], parts[1], parts[2]); // Each part is a new string
}
```

## Step 3: Implement a Reusable Buffer Pool

Create a custom buffer pool for message processing:

```csharp
public class MessageBufferPool
{
    private readonly ArrayPool<byte> _pool;
    private readonly int _defaultSize;
    private long _rented;
    private long _returned;

    public MessageBufferPool(int defaultSize = 4096)
    {
        _pool = ArrayPool<byte>.Create(maxArrayLength: defaultSize * 4, maxArraysPerBucket: 50);
        _defaultSize = defaultSize;
    }

    public byte[] Rent(int minimumSize = 0)
    {
        Interlocked.Increment(ref _rented);
        return _pool.Rent(Math.Max(minimumSize, _defaultSize));
    }

    public void Return(byte[] buffer, bool clearArray = false)
    {
        Interlocked.Increment(ref _returned);
        _pool.Return(buffer, clearArray);
    }

    public long ActiveRentals => Interlocked.Read(ref _rented) - Interlocked.Read(ref _returned);
    public long TotalRented => Interlocked.Read(ref _rented);
}
```

## Step 4: Configure and Compare GC Modes

Measure the impact of different GC latency modes:

```csharp
// Test different GC latency modes
var gcModes = new[]
{
    ("Interactive", GCLatencyMode.Interactive),
    ("LowLatency", GCLatencyMode.LowLatency),
    ("SustainedLowLatency", GCLatencyMode.SustainedLowLatency),
    ("Batch", GCLatencyMode.Batch),
};

foreach (var (name, mode) in gcModes)
{
    var previousMode = GCSettings.LatencyMode;
    GCSettings.LatencyMode = mode;

    var metrics = await RunWorkload(messageCount);

    GCSettings.LatencyMode = previousMode;
    Console.WriteLine($"{name}: {metrics.MessagesPerSecond:N0} msg/s, P99: {metrics.GetPercentile(99):F3}ms");
}
```

## Step 5: Measure GC Impact on Latency

Track GC collections during workload:

```csharp
static GcSnapshot GetGcSnapshot()
{
    return new GcSnapshot(
        GC.CollectionCount(0),
        GC.CollectionCount(1),
        GC.CollectionCount(2),
        GC.GetTotalMemory(false)
    );
}

var before = GetGcSnapshot();
// ... run workload ...
var after = GetGcSnapshot();

Console.WriteLine($"Gen0 collections: {after.Gen0 - before.Gen0}");
Console.WriteLine($"Gen1 collections: {after.Gen1 - before.Gen1}");
Console.WriteLine($"Gen2 collections: {after.Gen2 - before.Gen2}");
Console.WriteLine($"Memory delta: {(after.TotalMemory - before.TotalMemory) / 1024.0:F1} KB");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- ArrayPool<T> vs raw allocation comparison
- Zero-copy parsing with Span<T>
- Custom buffer pool implementation
- GC latency mode comparison
- GC collection tracking and analysis

## Sample Output

```
=== Surgewave Bootcamp: Lesson 26.4 - Memory Management & GC Tuning ===

Surgewave broker started on port 12345

--- Allocation vs Pooling Comparison ---
With Allocations:  45,200 msg/s, Allocated: 125.4 MB, GC Gen0: 42
With Pooling:      68,400 msg/s, Allocated:   2.1 MB, GC Gen0: 1
Improvement: 51.3% faster, 98.3% less allocation

--- Zero-Copy Parsing ---
String.Split():   12,400 msg/s, Allocated: 85.2 MB
Span<T> Parsing:  38,600 msg/s, Allocated:  0.1 MB
Improvement: 211.3% faster

--- GC Latency Mode Comparison ---
Interactive:           52,100 msg/s, P99: 0.450ms
LowLatency:            54,800 msg/s, P99: 0.320ms
SustainedLowLatency:   56,200 msg/s, P99: 0.280ms
Batch:                 61,400 msg/s, P99: 0.850ms

--- GC Collection Analysis ---
                 Gen0  Gen1  Gen2  Memory Delta
Allocations:       42     8     2    +125.4 MB
Pooling:            1     0     0      +2.1 MB

Lesson complete!
```

## Knowledge Check

1. When should you use Span<T> vs Memory<T>?
2. Why is ArrayPool<T>.Return() important for performance?
3. What is the difference between ServerGC and WorkstationGC?
4. When would you use GCLatencyMode.Batch vs SustainedLowLatency?
5. How does zero-copy processing affect P99 latency?

---

**Next:** [Lesson 26.5: Benchmarking & Profiling](../lesson-5/README.md)
