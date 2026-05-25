# Lesson 2.5: Benchmarking

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 2.4

## Overview

In this lesson, you'll learn how to benchmark Surgewave applications using BenchmarkDotNet. Performance testing helps you understand throughput, latency, and resource usage of your messaging patterns.

## Learning Objectives

By the end of this lesson, you will:

1. Set up BenchmarkDotNet for Surgewave benchmarks
2. Write producer throughput benchmarks
3. Write consumer latency benchmarks
4. Interpret benchmark results
5. Establish performance baselines

## Key Concepts

### Why Benchmark?

- **Baseline**: Know your system's normal performance
- **Regression Detection**: Catch slowdowns early
- **Capacity Planning**: Understand limits
- **Optimization**: Measure improvement attempts

### BenchmarkDotNet Setup

```xml
<PackageReference Include="BenchmarkDotNet" Version="0.14.*" />
```

### Basic Benchmark Class

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ProducerBenchmarks
{
    private ISurgewaveRuntime _runtime = null!;
    private SurgewaveProducer<string, string> _producer = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageMode(StorageMode.Memory)
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

    [Benchmark]
    public async Task Produce_SingleMessage()
    {
        await _producer.ProduceAsync("bench-topic", "key", "value");
    }
}
```

### Key Metrics

| Metric | Description |
|--------|-------------|
| Mean | Average execution time |
| StdDev | Variation in execution time |
| Allocated | Memory allocated per operation |
| Op/s | Operations per second |

## Prerequisites

- Completed Lesson 2.4: Testcontainers

## Getting Started

1. Navigate to the `start` folder
2. Open `Benchmarks.cs` and follow the TODO comments
3. Reference `INSTRUCTIONS.md` for detailed guidance

## Verification

Run the benchmarks:

```bash
cd completed
dotnet run -c Release
```

Expected output shows benchmark results table.

## Next Steps

Congratulations! You've completed **Unit 2: Testing Surgewave**.

Continue to [Unit 28: Advanced Messaging](../../unit-28/README.md) to learn about serialization, transactions, and more!
