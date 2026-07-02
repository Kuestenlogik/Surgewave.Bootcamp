// Surgewave Bootcamp - Unit 26, Lesson 4: Memory Management & GC Tuning
// COMPLETED SOLUTION

using System.Buffers;
using System.Diagnostics;
using System.Runtime;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 26.4 - Memory Management & GC Tuning ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
Console.WriteLine($"GC Server Mode: {GCSettings.IsServerGC}");
Console.WriteLine($"GC Latency Mode: {GCSettings.LatencyMode}");

var bootstrapServers = runtime.BootstrapServers;
var topic = "memory-gc-test";

// Configuration
var messageCount = 50_000;

// Step 1 - Compare allocation-heavy vs pooled approaches
Console.WriteLine("\n--- Allocation vs Pooling Comparison ---");

var gcBeforeAlloc = GetGcSnapshot();
var (allocMetrics, allocBytes) = MeasureWithAllocations(messageCount);
var gcAfterAlloc = GetGcSnapshot();

var gcBeforePool = GetGcSnapshot();
var (poolMetrics, poolBytes) = MeasureWithPooling(messageCount);
var gcAfterPool = GetGcSnapshot();

Console.WriteLine($"With Allocations: {allocMetrics.MessagesPerSecond,8:N0} msg/s, Allocated: {allocBytes / 1024.0 / 1024.0:F1} MB, " +
    $"GC Gen0: {gcAfterAlloc.Gen0 - gcBeforeAlloc.Gen0}");
Console.WriteLine($"With Pooling:     {poolMetrics.MessagesPerSecond,8:N0} msg/s, Allocated: {poolBytes / 1024.0 / 1024.0:F1} MB, " +
    $"GC Gen0: {gcAfterPool.Gen0 - gcBeforePool.Gen0}");

var speedImprovement = (poolMetrics.MessagesPerSecond / allocMetrics.MessagesPerSecond - 1) * 100;
var allocReduction = (1 - (double)poolBytes / allocBytes) * 100;
Console.WriteLine($"Improvement: {speedImprovement:F1}% faster, {allocReduction:F1}% less allocation");

// Step 2 - Zero-copy parsing comparison
Console.WriteLine("\n--- Zero-Copy Parsing ---");

var testMessages = new string[messageCount];
for (int i = 0; i < messageCount; i++)
{
    testMessages[i] = $"key-{i}|This is a test payload with some data for message number {i}|2025-01-15T10:30:00Z";
}

var gcBeforeSplit = GetGcSnapshot();
var allocBefore = GC.GetTotalAllocatedBytes(precise: true);
var splitMetrics = MeasureSplitParsing(testMessages);
var allocAfterSplit = GC.GetTotalAllocatedBytes(precise: true) - allocBefore;
var gcAfterSplit = GetGcSnapshot();

var gcBeforeSpan = GetGcSnapshot();
allocBefore = GC.GetTotalAllocatedBytes(precise: true);
var spanMetrics = MeasureSpanParsing(testMessages);
var allocAfterSpan = GC.GetTotalAllocatedBytes(precise: true) - allocBefore;
var gcAfterSpan = GetGcSnapshot();

Console.WriteLine($"String.Split(): {splitMetrics.MessagesPerSecond,8:N0} msg/s, Allocated: {allocAfterSplit / 1024.0 / 1024.0:F1} MB, " +
    $"GC Gen0: {gcAfterSplit.Gen0 - gcBeforeSplit.Gen0}");
Console.WriteLine($"Span<T> Parse:  {spanMetrics.MessagesPerSecond,8:N0} msg/s, Allocated: {allocAfterSpan / 1024.0 / 1024.0:F1} MB, " +
    $"GC Gen0: {gcAfterSpan.Gen0 - gcBeforeSpan.Gen0}");

var parseImprovement = (spanMetrics.MessagesPerSecond / splitMetrics.MessagesPerSecond - 1) * 100;
Console.WriteLine($"Improvement: {parseImprovement:F1}% faster");

// Step 3 - Custom buffer pool
Console.WriteLine("\n--- Custom Buffer Pool ---");
var bufferPool = new MessageBufferPool(defaultSize: 1024);

for (int i = 0; i < 1000; i++)
{
    var buffer = bufferPool.Rent();
    FillBuffer(buffer, i);
    ProcessBufferInPlace(buffer.AsSpan(0, 1024));
    bufferPool.Return(buffer);
}

Console.WriteLine($"Total rentals: {bufferPool.TotalRented:N0}");
Console.WriteLine($"Active rentals: {bufferPool.ActiveRentals}");

// Step 4 - GC latency mode comparison
Console.WriteLine("\n--- GC Latency Mode Comparison ---");

var gcModes = new (string Name, GCLatencyMode Mode)[]
{
    ("Interactive", GCLatencyMode.Interactive),
    ("LowLatency", GCLatencyMode.LowLatency),
    ("SustainedLowLatency", GCLatencyMode.SustainedLowLatency),
    ("Batch", GCLatencyMode.Batch),
};

Console.WriteLine($"{"Mode",-25} {"Throughput",12} {"P50",10} {"P99",10}");
Console.WriteLine(new string('-', 60));

foreach (var (name, mode) in gcModes)
{
    var previousMode = GCSettings.LatencyMode;
    try
    {
        GCSettings.LatencyMode = mode;
    }
    catch (Exception)
    {
        Console.WriteLine($"{name,-25} (not supported in this environment)");
        continue;
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();

    var metrics = RunWorkloadWithPooling(messageCount);

    Console.WriteLine($"{name,-25} {metrics.MessagesPerSecond,9:N0} msg/s {metrics.GetPercentile(50),7:F3}ms {metrics.GetPercentile(99),7:F3}ms");

    GCSettings.LatencyMode = previousMode;
}

// Step 5 - End-to-end Surgewave test with GC tracking
Console.WriteLine("\n--- Surgewave Producer/Consumer with GC Tracking ---");

var testTopic = "memory-test-e2e";
var e2eCount = 5_000;

// Produce with GC tracking
var gcBeforeProduce = GetGcSnapshot();
var produceMetrics = new PerformanceMetrics();

await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
});

produceMetrics.Start();
for (int i = 0; i < e2eCount; i++)
{
    var start = Stopwatch.GetTimestamp();
    await producer.ProduceAsync(testTopic, $"key-{i}", new string('x', 512));
    var elapsed = Stopwatch.GetElapsedTime(start);
    produceMetrics.RecordMessage(512, elapsed.TotalMilliseconds);
}
produceMetrics.Stop();
var gcAfterProduce = GetGcSnapshot();

// Consume with GC tracking
var gcBeforeConsume = GetGcSnapshot();
var consumeMetrics = new PerformanceMetrics();

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "memory-test-group";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
consumer.Subscribe(testTopic);

consumeMetrics.Start();
var consumed = 0;
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    while (consumed < e2eCount && !cts.Token.IsCancellationRequested)
    {
        var start = Stopwatch.GetTimestamp();
        var result = await consumer.ConsumeAsync(cts.Token);
        if (result?.Value != null)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            consumeMetrics.RecordMessage(result.Value.Length, elapsed.TotalMilliseconds);
            consumed++;
        }
    }
}
catch (OperationCanceledException) { }
consumeMetrics.Stop();
var gcAfterConsume = GetGcSnapshot();

Console.WriteLine($"\nProducer: {produceMetrics.MessagesPerSecond:N0} msg/s, P99: {produceMetrics.GetPercentile(99):F3}ms");
Console.WriteLine($"Consumer: {consumeMetrics.MessagesPerSecond:N0} msg/s, P99: {consumeMetrics.GetPercentile(99):F3}ms");

// Step 6 - GC collection summary
Console.WriteLine("\n--- GC Collection Summary ---");
Console.WriteLine($"{"Phase",-15} {"Gen0",6} {"Gen1",6} {"Gen2",6} {"Memory Delta",15}");
Console.WriteLine(new string('-', 50));

PrintGcDelta("Produce", gcBeforeProduce, gcAfterProduce);
PrintGcDelta("Consume", gcBeforeConsume, gcAfterConsume);

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use ArrayPool<T> to avoid repeated buffer allocations");
Console.WriteLine("2. Use Span<T> for zero-copy parsing and slicing");
Console.WriteLine("3. Use Memory<T> when you need heap storage or async methods");
Console.WriteLine("4. ServerGC + SustainedLowLatency is ideal for Surgewave brokers");
Console.WriteLine("5. Monitor Gen2 collections - they cause the longest pauses");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// --- Helper Methods ---

(PerformanceMetrics Metrics, long AllocatedBytes) MeasureWithAllocations(int count)
{
    var metrics = new PerformanceMetrics();
    var initialBytes = GC.GetTotalAllocatedBytes(precise: true);

    metrics.Start();
    for (int i = 0; i < count; i++)
    {
        var start = Stopwatch.GetTimestamp();

        // Each iteration allocates a new byte array
        var buffer = new byte[1024];
        FillBuffer(buffer, i);
        var result = ProcessBufferCopy(buffer); // Allocates result

        var elapsed = Stopwatch.GetElapsedTime(start);
        metrics.RecordMessage(buffer.Length, elapsed.TotalMilliseconds);
    }
    metrics.Stop();

    var totalAllocated = GC.GetTotalAllocatedBytes(precise: true) - initialBytes;
    return (metrics, totalAllocated);
}

(PerformanceMetrics Metrics, long AllocatedBytes) MeasureWithPooling(int count)
{
    var metrics = new PerformanceMetrics();
    var pool = ArrayPool<byte>.Shared;
    var initialBytes = GC.GetTotalAllocatedBytes(precise: true);

    metrics.Start();
    for (int i = 0; i < count; i++)
    {
        var start = Stopwatch.GetTimestamp();

        var buffer = pool.Rent(1024);
        try
        {
            FillBuffer(buffer, i);
            ProcessBufferInPlace(buffer.AsSpan(0, 1024)); // No allocation
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

PerformanceMetrics MeasureSplitParsing(string[] messages)
{
    var metrics = new PerformanceMetrics();
    metrics.Start();

    foreach (var message in messages)
    {
        var start = Stopwatch.GetTimestamp();

        // Allocation-heavy: Split creates string[] and substrings
        var parts = message.Split('|');
        var key = parts[0];
        var payload = parts[1];
        var timestamp = parts[2];

        // Simulate using the values
        var totalLen = key.Length + payload.Length + timestamp.Length;

        var elapsed = Stopwatch.GetElapsedTime(start);
        metrics.RecordMessage(message.Length, elapsed.TotalMilliseconds);
    }

    metrics.Stop();
    return metrics;
}

PerformanceMetrics MeasureSpanParsing(string[] messages)
{
    var metrics = new PerformanceMetrics();
    metrics.Start();

    foreach (var message in messages)
    {
        var start = Stopwatch.GetTimestamp();

        // Zero-copy: Span slicing, no allocations
        var span = message.AsSpan();
        var firstSep = span.IndexOf('|');
        var lastSep = span.LastIndexOf('|');

        var key = span[..firstSep];
        var payload = span[(firstSep + 1)..lastSep];
        var timestamp = span[(lastSep + 1)..];

        // Simulate using the values
        var totalLen = key.Length + payload.Length + timestamp.Length;

        var elapsed = Stopwatch.GetElapsedTime(start);
        metrics.RecordMessage(message.Length, elapsed.TotalMilliseconds);
    }

    metrics.Stop();
    return metrics;
}

PerformanceMetrics RunWorkloadWithPooling(int count)
{
    var metrics = new PerformanceMetrics();
    var pool = ArrayPool<byte>.Shared;

    metrics.Start();
    for (int i = 0; i < count; i++)
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

    return metrics;
}

static void FillBuffer(byte[] buffer, int seed)
{
    var value = (byte)(seed & 0xFF);
    for (int i = 0; i < Math.Min(buffer.Length, 1024); i++)
    {
        buffer[i] = value;
    }
}

static byte[] ProcessBufferCopy(byte[] input)
{
    // Allocates a new array for the result
    var output = new byte[input.Length];
    for (int i = 0; i < input.Length; i++)
    {
        output[i] = (byte)(input[i] ^ 0xAA);
    }
    return output;
}

static void ProcessBufferInPlace(Span<byte> buffer)
{
    // Zero allocation - modifies in place
    for (int i = 0; i < buffer.Length; i++)
    {
        buffer[i] = (byte)(buffer[i] ^ 0xAA);
    }
}

static GcSnapshot GetGcSnapshot() =>
    new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetTotalMemory(false));

static void PrintGcDelta(string phase, GcSnapshot before, GcSnapshot after)
{
    var memDelta = after.TotalMemory - before.TotalMemory;
    var sign = memDelta >= 0 ? "+" : "";
    Console.WriteLine($"{phase,-15} {after.Gen0 - before.Gen0,6} {after.Gen1 - before.Gen1,6} {after.Gen2 - before.Gen2,6} {sign}{memDelta / 1024.0:F1} KB");
}

// Supporting types

public record GcSnapshot(int Gen0, int Gen1, int Gen2, long TotalMemory);

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

    public long MessageCount => _messageCount;
    public TimeSpan Elapsed => _stopwatch.Elapsed;
}