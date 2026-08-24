# Knowledge Assessment: Lesson 2.5 - Benchmarking

## Multiple Choice

### 1. What is the primary purpose of benchmarking Surgewave applications?
- [ ] A) To find bugs in the code
- [ ] B) To establish performance baselines and detect regressions
- [ ] C) To verify message delivery
- [ ] D) To test error handling

<details>
<summary>Answer</summary>
**B)** To establish performance baselines and detect regressions. Benchmarking helps you understand your system's normal performance and catch slowdowns early.
</details>

### 2. Which attribute enables memory allocation tracking in BenchmarkDotNet?
- [ ] A) `[Benchmark]`
- [ ] B) `[MemoryDiagnoser]`
- [ ] C) `[SimpleJob]`
- [ ] D) `[GlobalSetup]`

<details>
<summary>Answer</summary>
**B)** `[MemoryDiagnoser]`. This attribute enables tracking of memory allocations per benchmark operation, helping identify memory-intensive code paths.
</details>

### 3. What does the "Mean" metric represent in benchmark results?
- [ ] A) The fastest execution time
- [ ] B) The average execution time across all iterations
- [ ] C) The middle value when sorted
- [ ] D) The most common execution time

<details>
<summary>Answer</summary>
**B)** The average execution time across all iterations. Mean is the arithmetic average of all measurements, providing a general sense of typical performance.
</details>

### 4. What is the purpose of `[GlobalSetup]` in a benchmark class?
- [ ] A) To run code before each benchmark method
- [ ] B) To run code once before all benchmarks in the class
- [ ] C) To configure BenchmarkDotNet settings
- [ ] D) To generate test data

<details>
<summary>Answer</summary>
**B)** To run code once before all benchmarks in the class. Use `[GlobalSetup]` for expensive setup like starting a broker, which should happen once rather than per benchmark iteration.
</details>

## True/False

### 5. Benchmarks should be run in Debug configuration for accurate results.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Benchmarks should be run in Release configuration (`dotnet run -c Release`). Debug builds include extra checks and disable optimizations, giving misleading performance numbers.
</details>

### 6. The StdDev (Standard Deviation) metric indicates variation in execution times.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - StdDev measures how much execution times vary from the mean. A high StdDev indicates inconsistent performance, while a low StdDev indicates stable performance.
</details>

## Code Challenge

### 7. Complete this benchmark class for measuring producer throughput:

```csharp
[____________]  // Enable memory tracking
[SimpleJob(RuntimeMoniker.Net80)]
public class ProducerBenchmarks
{
    private ISurgewaveRuntime _runtime = null!;
    private SurgewaveProducer<string, string> _producer = null!;

    [____________]  // Run once before all benchmarks
    public async Task Setup()
    {
        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .Build()
            .StartAsync();

        _producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_runtime.Port}");
    }

    [____________]  // Run once after all benchmarks
    public async Task Cleanup()
    {
        await _producer.DisposeAsync();
        await _runtime.DisposeAsync();
    }

    [____________]  // Mark this as a benchmark method
    public async Task Produce_SingleMessage()
    {
        await _producer.ProduceAsync("bench-topic", "key", "value");
    }

    [____________]  // Another benchmark method
    public async Task Produce_BatchOf100()
    {
        for (int i = 0; i < 100; i++)
            await _producer.ProduceAsync("bench-topic", $"key-{i}", $"value-{i}");
        await _producer.FlushAsync();
    }
}
```

<details>
<summary>Answer</summary>
```csharp
[MemoryDiagnoser]  // Enable memory tracking
[SimpleJob(RuntimeMoniker.Net80)]
public class ProducerBenchmarks
{
    private ISurgewaveRuntime _runtime = null!;
    private SurgewaveProducer<string, string> _producer = null!;

    [GlobalSetup]  // Run once before all benchmarks
    public async Task Setup()
    {
        _runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(0)
            .WithStorageEngine(StorageEngines.Memory)
            .Build()
            .StartAsync();

        _producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = $"localhost:{_runtime.Port}");
    }

    [GlobalCleanup]  // Run once after all benchmarks
    public async Task Cleanup()
    {
        await _producer.DisposeAsync();
        await _runtime.DisposeAsync();
    }

    [Benchmark]  // Mark this as a benchmark method
    public async Task Produce_SingleMessage()
    {
        await _producer.ProduceAsync("bench-topic", "key", "value");
    }

    [Benchmark]  // Another benchmark method
    public async Task Produce_BatchOf100()
    {
        for (int i = 0; i < 100; i++)
            await _producer.ProduceAsync("bench-topic", $"key-{i}", $"value-{i}");
        await _producer.FlushAsync();
    }
}
```

Key attributes:
- `[MemoryDiagnoser]` - tracks memory allocations
- `[GlobalSetup]` - one-time setup before benchmarks
- `[GlobalCleanup]` - one-time cleanup after benchmarks
- `[Benchmark]` - marks methods as benchmark targets
</details>

## Score
- 7/7: Excellent! You understand performance benchmarking
- 5-6/7: Good grasp of BenchmarkDotNet basics
- 3-4/7: Review benchmark attributes and metrics
- <3/7: Re-do the lesson and run some benchmarks
