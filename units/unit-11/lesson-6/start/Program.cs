// Surgewave Bootcamp - Unit 5, Lesson 6: Performance Comparison & Validation
//
// In this lesson, you'll learn:
// - How to benchmark Surgewave vs Kafka (throughput, P50, P99 latency)
// - Expected performance differences and what drives them
// - Common migration pitfalls and how to fix them
// - Verification checklist for production cutover
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 5.6 - Performance Comparison & Validation ===");
        Console.WriteLine();

        // Start two brokers: Surgewave (9092) and simulated Kafka (9093)
        await using var surgewave = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        await using var kafka = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9093)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine("Surgewave: localhost:9092 | Kafka (sim): localhost:9093");
        Console.WriteLine();

        // TODO 1: Implement RunBenchmarkAsync(bootstrap, label, messageCount)
        //         Produce 10_000 x 1 KB messages and measure:
        //         - Total throughput in MB/s
        //         - P50 and P99 latency in ms
        //         Return a BenchmarkResult record

        // TODO 2: Run a warm-up pass on both brokers (500 messages each)

        // TODO 3: Run the real benchmark on both Kafka (9093) and Surgewave (9092)
        //         Print results side by side

        // TODO 4: Show the three common pitfalls:
        //         a) Wrong partition count — create topics explicitly
        //         b) Dangerous AutoOffsetReset — always set explicitly
        //         c) LingerMs = 0 vs 5 — demonstrate the throughput difference

        // TODO 5: Run the pre-cutover checklist against Surgewave (9092) for topic "orders"
        //         Steps: broker reachable, topic exists, round-trip produce/consume

        // TODO 6: Print a migration summary comparing the benchmark results

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}
