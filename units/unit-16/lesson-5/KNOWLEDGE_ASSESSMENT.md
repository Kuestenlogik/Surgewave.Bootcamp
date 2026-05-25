# Knowledge Assessment: Lesson 25.5 - Pipeline Monitoring and Debugging

## Multiple Choice

### 1. Which metric is most useful for identifying the slowest node in a pipeline?
- [ ] A) Total messages processed
- [ ] B) P99 latency per node
- [ ] C) Total bytes processed
- [ ] D) Error count

<details>
<summary>Answer</summary>
**B)** P99 latency per node shows the processing time that 99% of messages complete within. The node with the highest P99 latency is typically the bottleneck. Total messages processed tells you volume but not speed.
</details>

### 2. What does a high P99 but low P50 latency indicate?
- [ ] A) The node is consistently slow
- [ ] B) The node is consistently fast
- [ ] C) Most messages are fast, but occasional messages experience high latency (tail latency)
- [ ] D) The metrics are incorrect

<details>
<summary>Answer</summary>
**C)** A large gap between P50 (median) and P99 indicates tail latency - most messages process quickly, but some outliers take much longer. Common causes include garbage collection pauses, network timeouts, resource contention, or occasional expensive operations.
</details>

### 3. What is the purpose of message tracing?
- [ ] A) To count messages
- [ ] B) To follow a single message through all pipeline nodes for debugging
- [ ] C) To compress messages
- [ ] D) To encrypt messages

<details>
<summary>Answer</summary>
**B)** Message tracing follows a specific message through every node in the pipeline, recording the data transformation, latency, and any errors at each step. This is essential for debugging transformation issues, understanding data flow, and reproducing problems.
</details>

### 4. When should a pipeline node's health status be "CRIT" (critical)?
- [ ] A) When any single error occurs
- [ ] B) When the error rate exceeds a critical threshold or latency is extremely high
- [ ] C) When the node starts processing
- [ ] D) When throughput is above average

<details>
<summary>Answer</summary>
**B)** Critical status indicates that the node's error rate or latency has exceeded critical thresholds (e.g., >5% error rate, >1000ms P99 latency). A single error might warrant a "WARN" status, but critical means the pipeline is degraded and needs intervention.
</details>

### 5. How can you identify a bottleneck in a multi-node pipeline?
- [ ] A) Find the node with the most errors
- [ ] B) Find the node with the highest message count
- [ ] C) Find the node with the lowest throughput compared to its upstream neighbors
- [ ] D) Find the node with the smallest memory usage

<details>
<summary>Answer</summary>
**C)** A bottleneck is the node that limits overall pipeline throughput. It has lower throughput than nodes upstream from it, causing backpressure. Upstream nodes may show increased queue depth while downstream nodes show reduced throughput.
</details>

## True/False

### 6. P50 latency is always more important than P99 for production monitoring.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - P99 (and even P99.9) latency is often more important for production monitoring because it represents the experience of the worst-affected users/messages. A system with great P50 but terrible P99 may have serious issues (like timeouts or retries) that affect a significant number of requests.
</details>

### 7. Pipeline monitoring should only collect metrics when problems are suspected.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Metrics should be collected continuously, not only when problems occur. Continuous monitoring establishes baselines for normal behavior, enables trend analysis, allows early detection of degradation before it becomes critical, and provides historical data for post-incident analysis.
</details>

### 8. Backpressure metrics measure the queue depth between pipeline nodes.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Backpressure metrics measure how many messages are waiting in the buffer/queue between nodes. High backpressure indicates the downstream node cannot keep up with the upstream node's output rate, which helps identify bottlenecks.
</details>

## Code Challenge

### 9. What's wrong with this health check implementation?

```csharp
public static string GetNodeStatus(NodeMetrics metrics)
{
    if (metrics.ErrorCount > 0)
        return "CRIT";
    return "OK";
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Too aggressive on errors** - A single error triggers CRIT status. In production, occasional errors are normal. Error rate (errors/total) is more meaningful than absolute error count.

2. **Ignores latency** - High latency is a critical health indicator that's completely ignored.

3. **No WARNING level** - There's no graduated health status. Going directly from OK to CRIT misses the opportunity for early warnings.

4. **No throughput consideration** - A node with zero throughput (stuck/dead) would report "OK".

Corrected version:
```csharp
public static string GetNodeStatus(NodeMetrics metrics)
{
    var errorRate = metrics.MessagesProcessed > 0
        ? (double)metrics.ErrorCount / metrics.MessagesProcessed * 100
        : 0;

    var p99 = GetPercentile(metrics.LatencySamples, 99);

    // Check for zero throughput (dead node)
    if (metrics.MessagesProcessed == 0 && metrics.ErrorCount > 0)
        return "CRIT";

    // Check error rate thresholds
    if (errorRate > 5.0 || p99 > 1000) return "CRIT";
    if (errorRate > 1.0 || p99 > 500) return "WARN";
    if (metrics.ErrorCount > 0) return "WARN";

    return "OK";
}
```
</details>

### 10. Write a function to detect the bottleneck node in a pipeline.

<details>
<summary>Answer</summary>

```csharp
public static string? FindBottleneck(Dictionary<string, NodeMetrics> metrics)
{
    if (metrics.Count == 0) return null;

    string? bottleneckId = null;
    double lowestThroughput = double.MaxValue;

    foreach (var (nodeId, m) in metrics)
    {
        // Skip nodes with no messages
        if (m.MessagesProcessed == 0) continue;

        var throughput = GetThroughput(m);

        if (throughput < lowestThroughput)
        {
            lowestThroughput = throughput;
            bottleneckId = nodeId;
        }
    }

    return bottleneckId;
}

// Usage:
var bottleneck = FindBottleneck(collector.GetAllMetrics());
if (bottleneck != null)
{
    var metrics = collector.GetAllMetrics()[bottleneck];
    var throughput = GetThroughput(metrics);
    Console.WriteLine($"Bottleneck: {metrics.NodeName} ({throughput:F0} msg/s)");
}
```

Key considerations:
- Compare throughput across all nodes
- The bottleneck is the node with the lowest throughput
- Skip nodes with zero messages (not yet active)
- In practice, also consider queue depth between nodes
- A node might have low throughput because its upstream is slow (not a bottleneck itself)
</details>

## Score
- 10/10: Excellent! Ready for the Visual Pipeline project
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson
