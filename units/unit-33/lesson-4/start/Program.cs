// Surgewave Bootcamp - Unit 26, Lesson 4: Memory Management & GC Tuning
//
// In this lesson, you'll learn:
// - ArrayPool<T> for buffer reuse
// - Span<T> and Memory<T> for zero-copy processing
// - GC latency modes and their trade-offs
// - Measuring GC pressure and allocation rates
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

var bootstrapServers = runtime.BootstrapServers;
var topic = "memory-gc-test";

// Configuration
var messageCount = 50_000;

// TODO: Step 1 - Compare allocation-heavy vs pooled approaches
// Console.WriteLine("\n--- Allocation vs Pooling Comparison ---");
//
// var (allocMetrics, allocBytes) = MeasureWithAllocations(messageCount);
// var allocGcBefore = GetGcSnapshot();
// // ... run ...
// var allocGcAfter = GetGcSnapshot();
//
// var (poolMetrics, poolBytes) = MeasureWithPooling(messageCount);
// var poolGcBefore = GetGcSnapshot();
// // ... run ...
// var poolGcAfter = GetGcSnapshot();
//
// Console.WriteLine($"Allocations: {allocMetrics.MessagesPerSecond:N0} msg/s, Allocated: {allocBytes / 1024.0 / 1024.0:F1} MB");
// Console.WriteLine($"Pooling:     {poolMetrics.MessagesPerSecond:N0} msg/s, Allocated: {poolBytes / 1024.0 / 1024.0:F1} MB");

// TODO: Step 2 - Zero-copy parsing with Span<T>
// Console.WriteLine("\n--- Zero-Copy Parsing ---");
//
// Compare String.Split() vs Span<T> slicing for message parsing:
//
// var message = "key-123|Hello, this is a test payload with some data|2025-01-15T10:30:00Z";
//
// // Allocation-heavy
// var parts = message.Split('|');
// var key = parts[0];
// var payload = parts[1];
// var timestamp = parts[2];
//
// // Zero-copy
// var span = message.AsSpan();
// var firstSep = span.IndexOf('|');
// var lastSep = span.LastIndexOf('|');
// var keySpan = span[..firstSep];
// var payloadSpan = span[(firstSep + 1)..lastSep];
// var timestampSpan = span[(lastSep + 1)..];

// TODO: Step 3 - Implement a custom buffer pool
// public class MessageBufferPool
// {
//     private readonly ArrayPool<byte> _pool;
//     private long _rented;
//     private long _returned;
//
//     public byte[] Rent(int size) { ... }
//     public void Return(byte[] buffer) { ... }
//     public long ActiveRentals => _rented - _returned;
// }

// TODO: Step 4 - Compare GC latency modes
// Console.WriteLine("\n--- GC Latency Mode Comparison ---");
//
// var gcModes = new[]
// {
//     ("Interactive", GCLatencyMode.Interactive),
//     ("LowLatency", GCLatencyMode.LowLatency),
//     ("SustainedLowLatency", GCLatencyMode.SustainedLowLatency),
//     ("Batch", GCLatencyMode.Batch),
// };
//
// foreach (var (name, mode) in gcModes)
// {
//     var previousMode = GCSettings.LatencyMode;
//     GCSettings.LatencyMode = mode;
//     var metrics = RunWorkload(messageCount);
//     GCSettings.LatencyMode = previousMode;
//     Console.WriteLine($"{name}: {metrics.MessagesPerSecond:N0} msg/s");
// }

// TODO: Step 5 - Track GC collections
// Console.WriteLine("\n--- GC Collection Analysis ---");
//
// var before = GetGcSnapshot();
// // ... run workload ...
// var after = GetGcSnapshot();
//
// Console.WriteLine($"Gen0: {after.Gen0 - before.Gen0}");
// Console.WriteLine($"Gen1: {after.Gen1 - before.Gen1}");
// Console.WriteLine($"Gen2: {after.Gen2 - before.Gen2}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Use ArrayPool<T> to avoid repeated buffer allocations");
Console.WriteLine("2. Use Span<T> for zero-copy parsing and slicing");
Console.WriteLine("3. Use Memory<T> when you need heap storage or async methods");
Console.WriteLine("4. ServerGC + SustainedLowLatency is ideal for Surgewave brokers");
Console.WriteLine("5. Monitor Gen2 collections - they cause the longest pauses");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Implement GcSnapshot record
// public record GcSnapshot(int Gen0, int Gen1, int Gen2, long TotalMemory);
//
// static GcSnapshot GetGcSnapshot() =>
//     new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetTotalMemory(false));

// TODO: Implement PerformanceMetrics class (same as previous lessons)
// public class PerformanceMetrics { ... }