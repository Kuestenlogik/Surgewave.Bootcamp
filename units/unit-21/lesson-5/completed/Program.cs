// Surgewave Bootcamp - Unit 4, Lesson 5: Performance Tuning
// COMPLETED SOLUTION

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

        // Baseline producer (default settings)
        await using var baselineProducer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
        });

        // High-throughput producer
        await using var throughputProducer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.BatchSize = 65536;        // 64KB batches
            opt.LingerMs = 50;            // Wait up to 50ms
            // opt.CompressionType = CompressionType.Lz4; // TODO: CompressionType not yet available on SurgewaveProducerOptions
            // opt.BufferMemory = 67108864;  // 64MB buffer // TODO: BufferMemory not yet available on SurgewaveProducerOptions
            // opt.Acks = Acks.One; // TODO: Acks not yet available on SurgewaveProducerOptions
        });

        // Low-latency producer
        await using var latencyProducer = new SurgewaveProducer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{runtime.Port}";
            opt.BatchSize = 1;            // No batching
            opt.LingerMs = 0;             // Send immediately
            // opt.CompressionType = CompressionType.None; // TODO: CompressionType not yet available on SurgewaveProducerOptions
            // opt.Acks = Acks.One; // TODO: Acks not yet available on SurgewaveProducerOptions
        });

        Console.WriteLine($"Benchmarking with {messageCount:N0} messages ({payload.Length} bytes each)...");
        Console.WriteLine();

        // Warm up
        await Warmup(baselineProducer);
        await Warmup(throughputProducer);
        await Warmup(latencyProducer);

        // Benchmark each configuration
        Console.WriteLine("--- Baseline (defaults) ---");
        var baselineRate = await Benchmark(baselineProducer, messageCount, payload, "baseline-topic");
        Console.WriteLine($"  Rate: {baselineRate:N0} msg/sec");
        Console.WriteLine();

        Console.WriteLine("--- High Throughput (batch=64KB, linger=50ms, lz4) ---");
        var throughputRate = await Benchmark(throughputProducer, messageCount, payload, "throughput-topic");
        Console.WriteLine($"  Rate: {throughputRate:N0} msg/sec");
        Console.WriteLine($"  Improvement: {(throughputRate / baselineRate - 1) * 100:F1}%");
        Console.WriteLine();

        Console.WriteLine("--- Low Latency (batch=1, linger=0) ---");
        var latencyRate = await Benchmark(latencyProducer, messageCount, payload, "latency-topic");
        Console.WriteLine($"  Rate: {latencyRate:N0} msg/sec");
        Console.WriteLine();

        // Summary
        Console.WriteLine("--- Performance Summary ---");
        Console.WriteLine($"{"Configuration",-25} {"Rate (msg/sec)",15} {"Relative",10}");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"{"Baseline",-25} {baselineRate,15:N0} {1.0,10:F2}x");
        Console.WriteLine($"{"High Throughput",-25} {throughputRate,15:N0} {throughputRate / baselineRate,10:F2}x");
        Console.WriteLine($"{"Low Latency",-25} {latencyRate,15:N0} {latencyRate / baselineRate,10:F2}x");

        Console.WriteLine();
        Console.WriteLine("--- Tuning Recommendations ---");
        Console.WriteLine("High Throughput: Use for batch processing, analytics pipelines");
        Console.WriteLine("  - Large batches, compression, async acks");
        Console.WriteLine();
        Console.WriteLine("Low Latency: Use for real-time systems, trading");
        Console.WriteLine("  - Small batches, no compression, minimal buffering");
        Console.WriteLine();
        Console.WriteLine("Balanced: Use for most applications");
        Console.WriteLine("  - Moderate batching, snappy compression, Acks.All");

        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }

    private static async Task Warmup(SurgewaveProducer<string, string> producer)
    {
        for (int i = 0; i < 100; i++)
        {
            await producer.ProduceAsync("warmup", $"key-{i}", "warmup");
        }
        await producer.FlushAsync();
    }

    private static async Task<double> Benchmark(
        SurgewaveProducer<string, string> producer,
        int messageCount,
        string payload,
        string topic)
    {
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < messageCount; i++)
        {
            await producer.ProduceAsync(topic, $"key-{i}", payload);
        }
        await producer.FlushAsync();

        sw.Stop();

        var elapsed = sw.Elapsed.TotalSeconds;
        var rate = messageCount / elapsed;

        Console.WriteLine($"  Sent {messageCount:N0} messages in {elapsed:F2}s");

        return rate;
    }
}
