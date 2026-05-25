# Lesson 34.7: Benchmarking with BenchmarkDotNet

### Surgewave's 7 Benchmark Projects

| Project | Focus | Category |
|---------|-------|----------|
| `Kuestenlogik.Surgewave.Benchmarks` | Core operations (produce, fetch, serialize) | General |
| `Kuestenlogik.Surgewave.Benchmarks.Unit` | Individual component micro-benchmarks | Unit |
| `Kuestenlogik.Surgewave.Benchmarks.Storage` | Log segment read/write, compaction | Storage |
| `Kuestenlogik.Surgewave.Benchmarks.Integration` | End-to-end produce/consume flows | Integration |
| `Kuestenlogik.Surgewave.Benchmarks.Comparison` | Kafka vs Surgewave head-to-head | Comparison |
| `Kuestenlogik.Surgewave.Benchmarks.Latency` | P50/P90/P99 latency measurement | Latency |
| `Kuestenlogik.Surgewave.Benchmarks.Streams` | Stream processing throughput | Streams |

### Writing a Benchmark

```csharp
[MemoryDiagnoser]                        // Track allocations
[BenchmarkCategory("Unit")]              // Categorize for regression thresholds
public class Crc32CBenchmark
{
    private byte[] _data = null!;

    [Params(64, 256, 1024, 4096)]       // Test multiple input sizes
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[DataSize];
        Random.Shared.NextBytes(_data);
    }

    [Benchmark(Baseline = true)]
    public uint SoftwareCrc32C()
    {
        return Crc32C.ComputeSoftware(_data);
    }

    [Benchmark]
    public uint HardwareCrc32C()
    {
        return Crc32C.Compute(_data);
    }
}
```

### Running Benchmarks

```bash
# Run all benchmarks in Release mode
dotnet run -c Release --project benchmarks/Kuestenlogik.Surgewave.Benchmarks

# Run a specific benchmark class
dotnet run -c Release --project benchmarks/Kuestenlogik.Surgewave.Benchmarks -- --filter "*Crc32C*"

# Export results as JSON (for regression comparison)
dotnet run -c Release --project benchmarks/Kuestenlogik.Surgewave.Benchmarks -- --exporters json
```

### Interpreting Results

```
|          Method |  DataSize |        Mean |     Median |    StdDev |  Allocated |
|---------------- |---------- |------------:|-----------:|----------:|-----------:|
| SoftwareCrc32C  |        64 |    45.23 ns |   44.89 ns |  1.23 ns |        0 B |
|  HardwareCrc32C |        64 |     4.56 ns |    4.52 ns |  0.12 ns |        0 B |
| SoftwareCrc32C  |      4096 |  2,345.6 ns | 2,340.1 ns | 15.23 ns |        0 B |
|  HardwareCrc32C |      4096 |   234.5 ns  |  233.8 ns  |  2.34 ns |        0 B |
```

**Key columns:**
- **Mean** -- Average execution time (lower is better)
- **Median** -- Middle value, less sensitive to outliers
- **StdDev** -- Variation between runs (lower = more stable)
- **Allocated** -- Heap bytes allocated per operation (0 B = GC-free)

### Benchmark Categories

Surgewave uses `[BenchmarkCategory]` attributes aligned with the regression detector's category overrides:

| Category | Latency Threshold | Throughput Threshold | Allocation Threshold |
|----------|-------------------|----------------------|----------------------|
| Unit | 15% (default) | 10% (default) | 20% (default) |
| Storage | 20% (lenient) | 10% | 20% |
| Integration | 15% | 10% | 20% |
| Comparison | 15% | 10% | 20% |
| Latency | 10% (strict) | 10% | 20% |
| Streams | 15% | 10% | 20% |

---

[Back to Unit 33](../README.md)
