# Lesson 2.5: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Set Up Benchmark Project

Benchmarks run as a console app, not tests:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.*" />
    <!-- Surgewave references -->
  </ItemGroup>
</Project>
```

## Step 2: Create Benchmark Class Structure

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

[MemoryDiagnoser]
public class ProducerBenchmarks
{
    private ISurgewaveRuntime _runtime = null!;
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
}
```

## Step 3: Write Single Message Benchmark

```csharp
[Benchmark(Baseline = true)]
public async Task Produce_SingleMessage()
{
    await _producer.ProduceAsync(
        "bench-topic",
        $"key-{_messageCounter++}",
        "benchmark-value");
}
```

## Step 4: Write Batch Benchmark

```csharp
[Benchmark]
[Arguments(10)]
[Arguments(100)]
[Arguments(1000)]
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
```

## Step 5: Write Consumer Benchmark

```csharp
[MemoryDiagnoser]
public class ConsumerBenchmarks
{
    private ISurgewaveRuntime _runtime = null!;
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

    [Benchmark]
    public async Task<string?> Consume_SingleMessage()
    {
        var result = await _consumer.ConsumeAsync(TimeSpan.FromSeconds(1));
        return result?.Value;
    }
}
```

## Step 6: Run Benchmarks

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ProducerBenchmarks>();
        // Or run all benchmarks:
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
```

Run in Release mode:
```bash
dotnet run -c Release
```

## Step 7: Interpret Results

```
| Method              | Mean       | Error    | StdDev   | Allocated |
|-------------------- |-----------:|---------:|---------:|----------:|
| Produce_Single      |   125.3 us |  2.45 us |  3.12 us |     1.2 KB|
| Produce_Batch(10)   |   432.1 us |  8.21 us | 11.34 us |     8.4 KB|
| Produce_Batch(100)  | 2,145.3 us | 42.13 us | 58.21 us |    82.1 KB|
| Produce_Batch(1000) |18,234.5 us |156.32 us |201.45 us |   812.3 KB|
```

**Key observations:**
- Mean: Average time per operation
- Batch of 100 is ~17x faster than 100 single produces (2.1ms vs ~12.5ms)
- Memory allocation scales linearly

## Complete Solution

See `completed/Benchmarks.cs` for the full implementation.

## Running with Different Options

```bash
# Quick run (fewer iterations)
dotnet run -c Release -- --job short

# Export to various formats
dotnet run -c Release -- --exporters html json

# Filter specific benchmarks
dotnet run -c Release -- --filter *Producer*
```

## Knowledge Check

Before completing Unit 2, make sure you can answer:

1. Why must benchmarks run in Release mode?
2. What does `[MemoryDiagnoser]` measure?
3. How do you compare performance between approaches?
4. What's the purpose of `[GlobalSetup]` vs `[IterationSetup]`?

---

## Unit 2 Complete!

You've finished Unit 2: Testing Surgewave. You now know how to:

- Write unit tests with embedded broker
- Mock Surgewave interfaces for isolated tests
- Create integration tests with fixtures
- Use Testcontainers patterns
- Benchmark performance

**Continue to Unit 28** → [Advanced Messaging](../../unit-28/README.md)
