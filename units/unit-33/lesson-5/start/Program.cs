// Surgewave Bootcamp - Unit 26, Lesson 5: Benchmarking & Profiling
//
// In this lesson, you'll learn:
// - Building a benchmarking harness with warm-up and statistics
// - Measuring P50/P90/P99 latency percentiles accurately
// - Profiling GC behavior during workloads
// - Comparing optimization strategies with statistical rigor
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// TODO: Step 1 - Implement BenchmarkRunner with warm-up and multiple runs
// public class BenchmarkRunner
// {
//     private readonly int _warmupIterations;
//     private readonly int _measureIterations;
//     private readonly int _runs;
//
//     public BenchmarkRunner(int warmupIterations = 100, int measureIterations = 1000, int runs = 3) { ... }
//
//     public async Task<BenchmarkResult> RunAsync(string name, Func<int, Task> action)
//     {
//         // 1. Warm-up phase
//         // 2. Force GC
//         // 3. Multiple measurement runs
//         // 4. Collect latencies
//         // 5. Return BenchmarkResult with statistics
//     }
// }

// TODO: Step 2 - Implement BenchmarkResult with percentile calculation
// public class BenchmarkResult
// {
//     public string Name { get; }
//     public double MeanThroughput { get; }
//     public double StdDevThroughput { get; }
//     public double P50 { get; }
//     public double P90 { get; }
//     public double P99 { get; }
//     public double P999 { get; }
//
//     public void PrintReport() { ... }
// }

// TODO: Step 3 - Benchmark Surgewave producer operations
// var runner = new BenchmarkRunner(warmupIterations: 200, measureIterations: 5000, runs: 3);
//
// // Sequential produce
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = bootstrapServers;
// });
//
// var seqResult = await runner.RunAsync("Sequential Produce", async i =>
// {
//     await producer.ProduceAsync($"{topic}-seq", $"key-{i}", payload);
// });
// seqResult.PrintReport();

// TODO: Step 4 - Profile GC during benchmarks
// Console.WriteLine("\n--- GC Profile ---");
// var gcBefore = GcSnapshot.Capture();
// var allocBefore = GC.GetTotalAllocatedBytes(precise: true);
//
// // ... run benchmark ...
//
// var gcAfter = GcSnapshot.Capture();
// var allocAfter = GC.GetTotalAllocatedBytes(precise: true);
//
// Console.WriteLine($"Gen0: {gcAfter.Gen0 - gcBefore.Gen0}");
// Console.WriteLine($"Allocated: {(allocAfter - allocBefore) / 1024.0 / 1024.0:F1} MB");

// TODO: Step 5 - Compare strategies
// Console.WriteLine("\n--- Performance Comparison ---");
// Console.WriteLine($"{"Strategy",-25} {"Throughput",12} {"P50",8} {"P99",8} {"P99.9",8}");
// Console.WriteLine(new string('-', 65));

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Always warm up before measuring (JIT compilation)");
Console.WriteLine("2. Use Stopwatch.GetTimestamp() for high-resolution timing");
Console.WriteLine("3. Run multiple iterations for statistical significance");
Console.WriteLine("4. Report percentiles (P50, P99), not just averages");
Console.WriteLine("5. Track GC collections alongside latency measurements");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// TODO: Implement GcSnapshot record
// public record GcSnapshot(int Gen0, int Gen1, int Gen2, long TotalMemory)
// {
//     public static GcSnapshot Capture() =>
//         new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetTotalMemory(false));
// }