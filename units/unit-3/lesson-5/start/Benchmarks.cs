// Surgewave Bootcamp - Unit 2, Lesson 5: Benchmarking
//
// In this lesson, you'll learn to:
// - Use BenchmarkDotNet to measure Surgewave performance
// - Write producer and consumer benchmarks
// - Interpret benchmark results
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// TODO: Implement ProducerBenchmarks class
[MemoryDiagnoser]
public class ProducerBenchmarks
{
    private ISurgewaveRuntime _runtime = null!;
    private SurgewaveProducer<string, string> _producer = null!;
    private int _messageCounter;

    // TODO: Implement GlobalSetup to start broker and create producer
    [GlobalSetup]
    public async Task Setup()
    {
        throw new NotImplementedException();
    }

    // TODO: Implement GlobalCleanup to dispose resources
    [GlobalCleanup]
    public async Task Cleanup()
    {
        throw new NotImplementedException();
    }

    // TODO: Implement single message benchmark
    [Benchmark(Baseline = true)]
    public async Task Produce_SingleMessage()
    {
        // Hint: await _producer.ProduceAsync("bench-topic", key, value)
        throw new NotImplementedException();
    }

    // TODO: Implement batch benchmark with different sizes
    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    public async Task Produce_Batch(int batchSize)
    {
        // Hint: Loop batchSize times, then FlushAsync
        throw new NotImplementedException();
    }
}

// TODO: Implement ConsumerBenchmarks class
[MemoryDiagnoser]
public class ConsumerBenchmarks
{
    private ISurgewaveRuntime _runtime = null!;
    private SurgewaveProducer<string, string> _producer = null!;
    private SurgewaveConsumer<string, string> _consumer = null!;
    private const string Topic = "bench-consume";

    // TODO: Setup broker, producer, consumer, and pre-populate messages
    [GlobalSetup]
    public async Task Setup()
    {
        throw new NotImplementedException();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        throw new NotImplementedException();
    }

    // TODO: Implement consume benchmark
    [Benchmark]
    public async Task<string?> Consume_SingleMessage()
    {
        throw new NotImplementedException();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // Run producer benchmarks
        BenchmarkRunner.Run<ProducerBenchmarks>();

        // Uncomment to also run consumer benchmarks
        // BenchmarkRunner.Run<ConsumerBenchmarks>();
    }
}
