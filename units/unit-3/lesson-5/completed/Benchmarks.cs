// Surgewave Bootcamp - Unit 2, Lesson 5: Benchmarking
// COMPLETED SOLUTION

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

[MemoryDiagnoser]
public class ProducerBenchmarks
{
    private SurgewaveRuntime _runtime = null!;
    private SurgewaveProducer<string, string> _producer = null!;
    private int _messageCounter;

    [GlobalSetup]
    public async Task Setup()
    {
        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        _producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_runtime.Port}");
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _producer.DisposeAsync();
        await _runtime.DisposeAsync();
    }

    [Benchmark(Baseline = true)]
    public async Task Produce_SingleMessage()
    {
        await _producer.ProduceAsync(
            "bench-topic",
            $"key-{_messageCounter++}",
            "benchmark-value");
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    public async Task Produce_Batch(int batchSize)
    {
        for (int i = 0; i < batchSize; i++)
        {
            await _producer.ProduceAsync(
                "bench-batch",
                $"key-{_messageCounter++}",
                "benchmark-value");
        }
        await _producer.FlushAsync();
    }
}

[MemoryDiagnoser]
public class ConsumerBenchmarks
{
    private SurgewaveRuntime _runtime = null!;
    private SurgewaveProducer<string, string> _producer = null!;
    private SurgewaveConsumer<string, string> _consumer = null!;
    private const string Topic = "bench-consume";

    [GlobalSetup]
    public async Task Setup()
    {
        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        _producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_runtime.Port}");

        _consumer = new SurgewaveConsumer<string, string>(opt =>
        {
            opt.BootstrapServers = $"localhost:{_runtime.Port}";
            opt.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        // Pre-populate with messages
        for (int i = 0; i < 10000; i++)
        {
            await _producer.ProduceAsync(Topic, $"key-{i}", $"value-{i}");
        }

        _consumer.Subscribe(Topic);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _consumer.DisposeAsync();
        await _producer.DisposeAsync();
        await _runtime.DisposeAsync();
    }

    [Benchmark]
    public async Task<string?> Consume_SingleMessage()
    {
        var result = await _consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
        return result?.Value;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 2.5 - Benchmarking ===");
        Console.WriteLine();
        Console.WriteLine("Running Producer Benchmarks...");
        Console.WriteLine("(This will take a few minutes)");
        Console.WriteLine();

        var summary = BenchmarkRunner.Run<ProducerBenchmarks>();

        Console.WriteLine();
        Console.WriteLine("Benchmark complete! Results saved to BenchmarkDotNet.Artifacts/");
    }
}
