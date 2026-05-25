# Lesson 9.5: Performance Profiling

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 9.4

Identify bottlenecks and profile memory usage.

## Objectives

- Profile application performance with Stopwatch and timers
- Identify memory allocation patterns
- Use diagnostic tools to find bottlenecks

## Concepts

### Performance Profiling Areas

| Area | What to Measure | Tools |
|------|-----------------|-------|
| Throughput | Messages/second | Counters, Stopwatch |
| Latency | Time per operation | Histograms, Percentiles |
| Memory | Allocations, GC | GC events, Allocator |
| CPU | Time spent in code | Profiler, Sampling |

### Measuring Throughput

```csharp
var sw = Stopwatch.StartNew();
var count = 0;

while (sw.Elapsed < TimeSpan.FromSeconds(10))
{
    await producer.ProduceAsync(topic, key, value);
    count++;
}

var throughput = count / sw.Elapsed.TotalSeconds;
Console.WriteLine($"Throughput: {throughput:F2} msgs/sec");
```

### Measuring Latency Percentiles

```csharp
var latencies = new List<double>();

for (var i = 0; i < 1000; i++)
{
    var start = Stopwatch.GetTimestamp();
    await operation();
    var elapsed = Stopwatch.GetElapsedTime(start);
    latencies.Add(elapsed.TotalMilliseconds);
}

latencies.Sort();
var p50 = latencies[(int)(latencies.Count * 0.50)];
var p99 = latencies[(int)(latencies.Count * 0.99)];
```

### Memory Profiling

Monitor GC and allocations:

```csharp
var before = GC.GetTotalMemory(false);
// ... operation ...
var after = GC.GetTotalMemory(false);
var allocated = after - before;
```

## Exercise

In this lesson, you'll:
1. Measure producer/consumer throughput
2. Calculate latency percentiles (p50, p99)
3. Profile memory allocations

## Key Takeaways

- Measure before optimizing
- Focus on p99 latency, not just average
- Monitor allocations to reduce GC pressure
