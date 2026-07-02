// Surgewave Bootcamp - Unit 4, Lesson 5: Performance Tuning
//
// In this lesson, you'll learn to:
// - Configure batching and compression
// - Tune for throughput vs latency
// - Benchmark different configurations
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 4.5 - Performance Tuning ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageEngine(StorageEngines.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine($"Broker started on port {runtime.Port}");
        Console.WriteLine();

        var payload = new string('x', 1000); // 1KB message
        const int messageCount = 10000;

        // TODO: Create baseline producer (default settings)
        await using var baselineProducer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
        });

        // TODO: Create high-throughput producer
        // Hints: BatchSize, LingerMs, CompressionType, BufferMemory
        await using var throughputProducer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // TODO: Add throughput optimizations
        });

        // TODO: Create low-latency producer
        // Hints: BatchSize=1, LingerMs=0, no compression
        await using var latencyProducer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            // TODO: Add latency optimizations
        });

        // TODO: Benchmark each configuration
        Console.WriteLine($"Benchmarking with {messageCount} messages ({payload.Length} bytes each)...");
        Console.WriteLine();

        // var baselineRate = await Benchmark(baselineProducer, messageCount, payload);
        // var throughputRate = await Benchmark(throughputProducer, messageCount, payload);
        // var latencyRate = await Benchmark(latencyProducer, messageCount, payload);

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task<double> Benchmark(
        SurgewaveProducer<string, string> producer,
        int messageCount,
        string payload)
    {
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < messageCount; i++)
        {
            await producer.ProduceAsync("benchmark", $"key-{i}", payload);
        }
        await producer.FlushAsync();

        sw.Stop();
        return messageCount / sw.Elapsed.TotalSeconds;
    }
}
