# Knowledge Assessment: Lesson 9.5 - Performance Profiling

## Multiple Choice

### 1. Which percentile should you focus on to understand worst-case user experience?
- [ ] A) P50 (median)
- [ ] B) P75
- [ ] C) P99
- [ ] D) Average

<details>
<summary>Answer</summary>
**C)** P99 represents the 99th percentile - the latency that 99% of requests are faster than. This shows your worst-case experience (excluding extreme outliers) and is critical for understanding tail latency that affects real users.
</details>

### 2. What does GC.GetTotalMemory(false) return?
- [ ] A) Total RAM installed on the system
- [ ] B) Memory allocated by the current process
- [ ] C) Memory freed by garbage collection
- [ ] D) Maximum heap size

<details>
<summary>Answer</summary>
**B)** GC.GetTotalMemory(false) returns the total number of bytes currently allocated by the managed heap. The `false` parameter means it doesn't force a garbage collection before measuring.
</details>

### 3. When calculating throughput, what is the correct formula?
- [ ] A) Total Time / Message Count
- [ ] B) Message Count / Total Time in Seconds
- [ ] C) Message Size * Message Count
- [ ] D) Total Memory / Message Count

<details>
<summary>Answer</summary>
**B)** Throughput is calculated as Message Count / Total Time in Seconds, giving you messages per second (msgs/sec).
</details>

## True/False

### 4. You should optimize code before measuring its performance.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - "Measure before optimizing" is a key principle. Optimizing without measurement leads to wasted effort on non-bottlenecks. Profile first to identify actual performance issues.
</details>

### 5. Reducing allocations can improve performance by reducing GC pressure.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Fewer allocations mean less work for the garbage collector, resulting in fewer GC pauses and more consistent latency. This is especially important in high-throughput scenarios.
</details>

## Code Challenge

### 6. What's wrong with this throughput measurement?

```csharp
var count = 0;
var start = DateTime.Now;

for (int i = 0; i < 10000; i++)
{
    await producer.ProduceAsync(topic, key, value);
    count++;
}

var elapsed = DateTime.Now - start;
var throughput = count / elapsed.TotalSeconds;
```

<details>
<summary>Answer</summary>
The issue is using **DateTime.Now instead of Stopwatch**. DateTime.Now has poor resolution (typically 15ms on Windows) and can be affected by system clock changes. For performance measurement, always use Stopwatch.

Fix:
```csharp
var count = 0;
var sw = Stopwatch.StartNew();

for (int i = 0; i < 10000; i++)
{
    await producer.ProduceAsync(topic, key, value);
    count++;
}

sw.Stop();
var throughput = count / sw.Elapsed.TotalSeconds;
Console.WriteLine($"Throughput: {throughput:F2} msgs/sec");
```
</details>

### 7. Fix this percentile calculation:

```csharp
var latencies = new List<double>();

for (var i = 0; i < 1000; i++)
{
    var start = Stopwatch.GetTimestamp();
    await operation();
    var elapsed = Stopwatch.GetElapsedTime(start);
    latencies.Add(elapsed.TotalMilliseconds);
}

// Calculate p50 and p99
var p50 = latencies[500];   // Wrong!
var p99 = latencies[990];   // Wrong!
```

<details>
<summary>Answer</summary>
The issue is that **the list is not sorted** before accessing percentile indices. Percentiles require sorted data. Also, the indices should be calculated based on the actual count.

Fix:
```csharp
var latencies = new List<double>();

for (var i = 0; i < 1000; i++)
{
    var start = Stopwatch.GetTimestamp();
    await operation();
    var elapsed = Stopwatch.GetElapsedTime(start);
    latencies.Add(elapsed.TotalMilliseconds);
}

// MUST sort before calculating percentiles!
latencies.Sort();

// Calculate indices correctly
var p50 = latencies[(int)(latencies.Count * 0.50)];
var p99 = latencies[(int)(latencies.Count * 0.99)];

Console.WriteLine($"P50: {p50:F2}ms, P99: {p99:F2}ms");
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
