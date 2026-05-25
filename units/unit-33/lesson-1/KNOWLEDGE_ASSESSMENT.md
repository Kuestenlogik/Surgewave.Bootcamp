# Knowledge Assessment: Lesson 26.1 - Performance Fundamentals

## Multiple Choice

### 1. Which percentile represents the median latency?
- [ ] A) P90
- [ ] B) P99
- [ ] C) P50
- [ ] D) P99.9

<details>
<summary>Answer</summary>
**C)** P50 is the 50th percentile, meaning 50% of requests are faster than this value. It represents the median latency.
</details>

### 2. If your application handles 1 million requests per day with P99 = 100ms, approximately how many requests experience latency above 100ms?
- [ ] A) 100
- [ ] B) 1,000
- [ ] C) 10,000
- [ ] D) 100,000

<details>
<summary>Answer</summary>
**C)** P99 means 99% of requests are faster than 100ms. The remaining 1% of 1,000,000 = 10,000 requests experience latency above 100ms.
</details>

### 3. Which configuration prioritizes throughput over latency?
- [ ] A) BatchSize=1KB, LingerMs=0
- [ ] B) BatchSize=64KB, LingerMs=100
- [ ] C) BatchSize=1KB, LingerMs=100
- [ ] D) BatchSize=64KB, LingerMs=0

<details>
<summary>Answer</summary>
**B)** Large batch size (64KB) combined with high linger time (100ms) maximizes batching efficiency, trading individual message latency for higher overall throughput.
</details>

### 4. What is the primary cause of P99.9 latency spikes in .NET applications?
- [ ] A) Network latency
- [ ] B) Disk I/O
- [ ] C) Garbage Collection pauses
- [ ] D) DNS resolution

<details>
<summary>Answer</summary>
**C)** Garbage Collection (GC) pauses are the most common cause of tail latency spikes in .NET applications. Gen2 GC can pause all threads for several milliseconds.
</details>

### 5. Which metric best indicates consumer backlog?
- [ ] A) P99 latency
- [ ] B) Throughput
- [ ] C) Consumer lag
- [ ] D) Batch size

<details>
<summary>Answer</summary>
**C)** Consumer lag measures the number of messages that have been produced but not yet consumed, directly indicating processing backlog.
</details>

## True/False

### 6. Higher throughput always means better performance.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Performance depends on your requirements. For real-time applications, low latency may be more important than high throughput. The "best" performance depends on your use case.
</details>

### 7. P99 latency is always higher than P50 latency.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - By definition, P99 represents the latency at which 99% of requests are faster, while P50 represents 50%. Since P99 includes more samples, it will always be greater than or equal to P50.
</details>

### 8. Using Stopwatch.GetTimestamp() is more accurate than DateTime.Now for measuring latency.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Stopwatch uses high-resolution performance counters (often nanosecond precision), while DateTime.Now has millisecond resolution and can be affected by system clock changes.
</details>

## Code Challenge

### 9. What's wrong with this latency measurement code?

```csharp
var latencies = new List<double>();

for (int i = 0; i < 1000; i++)
{
    var start = DateTime.Now;
    await producer.ProduceAsync(topic, key, value);
    var elapsed = (DateTime.Now - start).TotalMilliseconds;
    latencies.Add(elapsed);
}

var p99 = latencies.OrderByDescending(x => x).Skip(10).First();
Console.WriteLine($"P99: {p99} ms");
```

<details>
<summary>Answer</summary>
There are three issues:

1. **Using DateTime.Now instead of Stopwatch**: DateTime.Now has low resolution (~15ms on Windows) and can be affected by time adjustments. Use `Stopwatch.GetTimestamp()` instead.

2. **Incorrect P99 calculation**: The code sorts descending and skips 10, which would give approximately P99 for 1000 items, but it's fragile. The correct approach is:
   ```csharp
   var sorted = latencies.OrderBy(x => x).ToList();
   var p99Index = (int)(sorted.Count * 0.99);
   var p99 = sorted[p99Index];
   ```

3. **No pre-warming**: The first few operations may include JIT compilation time. Add a warm-up phase before measuring.

Corrected code:
```csharp
var latencies = new List<double>();

// Warm-up
for (int i = 0; i < 100; i++)
    await producer.ProduceAsync(topic, key, value);

// Measure
for (int i = 0; i < 1000; i++)
{
    var start = Stopwatch.GetTimestamp();
    await producer.ProduceAsync(topic, key, value);
    var elapsed = Stopwatch.GetElapsedTime(start);
    latencies.Add(elapsed.TotalMilliseconds);
}

var sorted = latencies.OrderBy(x => x).ToList();
var p99 = sorted[(int)(sorted.Count * 0.99)];
Console.WriteLine($"P99: {p99:F3} ms");
```
</details>

### 10. Calculate the throughput given this scenario:

Your producer sends 100,000 messages of 2KB each in 5 seconds. What is:
- Messages per second?
- Megabytes per second?

<details>
<summary>Answer</summary>
**Messages per second:**
100,000 messages / 5 seconds = **20,000 msg/s**

**Megabytes per second:**
Total bytes = 100,000 * 2KB = 200,000 KB = 195.31 MB
Throughput = 195.31 MB / 5 seconds = **39.06 MB/s**

Note: 1 MB = 1024 KB, so 200,000 KB / 1024 = 195.31 MB
</details>

## Scenario Analysis

### 11. Your application shows these metrics:
- P50: 2ms
- P90: 5ms
- P99: 50ms
- P99.9: 500ms

What does this distribution indicate, and what should you investigate?

<details>
<summary>Answer</summary>
This distribution shows a **long-tail latency problem**. The large gap between P99 (50ms) and P99.9 (500ms) suggests:

**Likely causes:**
1. **GC pauses** - Gen2 garbage collection can cause multi-hundred millisecond pauses
2. **Resource contention** - Lock contention or thread pool exhaustion
3. **External dependencies** - Database or network timeouts

**Investigation steps:**
1. Enable GC logging to check for Gen2 collections
2. Profile for lock contention using dotTrace or PerfView
3. Add tracing to identify slow external calls
4. Check if P99.9 spikes correlate with specific times or load patterns

**Solutions might include:**
- Implement memory pooling to reduce allocations
- Use Server GC with low-latency mode
- Add async timeouts for external calls
- Scale out to reduce load per instance
</details>

## Score
- 11/11: Excellent! Ready for advanced optimization
- 8-10/11: Good understanding, review missed concepts
- 5-7/11: Review the lesson before continuing
- <5/11: Re-do the lesson and exercises
