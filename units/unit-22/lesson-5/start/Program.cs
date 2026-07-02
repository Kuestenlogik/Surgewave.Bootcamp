// Surgewave Bootcamp - Unit 9, Lesson 5: Performance Profiling
//
// In this lesson, you'll learn to:
// - Measure throughput and latency
// - Calculate latency percentiles (p50, p90, p99)
// - Profile memory allocations and GC
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text;
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

// TODO: Create a PerformanceProfiler
// Hint: var profiler = new PerformanceProfiler();

// TODO: Create a MemoryProfiler
// Hint: var memoryProfiler = new MemoryProfiler();

// TODO: Profile producer throughput
// Hint: profiler.Start();
//       for (var i = 0; i < 1000; i++)
//       {
//           var start = Stopwatch.GetTimestamp();
//           await producer.ProduceAsync(...);
//           var elapsed = Stopwatch.GetElapsedTime(start);
//           profiler.RecordOperation(elapsed);
//       }
//       profiler.Stop();

// TODO: Calculate and display latency percentiles
// Hint: var stats = profiler.GetLatencyStats();
//       Console.WriteLine($"P50: {stats.P50:F3}ms");
//       Console.WriteLine($"P99: {stats.P99:F3}ms");

// TODO: Profile memory usage
// Hint: memoryProfiler.Start();
//       // ... operations ...
//       var memStats = memoryProfiler.Stop();

// TODO: Generate a performance report

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Helper Classes ==============

// TODO: Implement PerformanceProfiler
// Hint: public class PerformanceProfiler
//       {
//           private readonly List<double> _latencies = new();
//           private readonly Stopwatch _totalTime = new();
//           public void RecordOperation(TimeSpan latency) { ... }
//           public LatencyStats GetLatencyStats() { ... }
//       }

// TODO: Implement MemoryProfiler
// Hint: public class MemoryProfiler
//       {
//           public void Start() { ... }
//           public MemoryStats Stop() { ... }
//       }

// TODO: Implement LatencyStats and MemoryStats records
