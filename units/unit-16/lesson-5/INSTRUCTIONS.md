# Lesson 25.5: Instructions

Follow these step-by-step instructions to implement pipeline monitoring and debugging.

## Step 1: Define Metrics Types

Create models for pipeline metrics:

```csharp
public record NodeMetrics
{
    public string NodeId { get; init; } = "";
    public string NodeName { get; init; } = "";
    public long MessagesProcessed { get; set; }
    public long MessagesDropped { get; set; }
    public long ErrorCount { get; set; }
    public long BytesProcessed { get; set; }
    public List<double> LatencySamples { get; } = new();
    public DateTime FirstMessageTime { get; set; } = DateTime.MaxValue;
    public DateTime LastMessageTime { get; set; } = DateTime.MinValue;
}

public record PipelineHealth(
    string PipelineId,
    string Status,
    Dictionary<string, NodeHealth> NodeHealthMap,
    double OverallThroughput,
    double OverallLatencyP50,
    double OverallLatencyP99
);

public record NodeHealth(
    string NodeId,
    string Status,
    double Throughput,
    double LatencyP50,
    double LatencyP99,
    double ErrorRate
);
```

## Step 2: Implement Metrics Collector

Build a collector that tracks per-node metrics:

```csharp
public class PipelineMetricsCollector
{
    private readonly Dictionary<string, NodeMetrics> _metrics = new();
    private readonly object _lock = new();

    public void RecordProcessed(string nodeId, string nodeName, long bytes, double latencyMs)
    {
        lock (_lock)
        {
            if (!_metrics.TryGetValue(nodeId, out var m))
            {
                m = new NodeMetrics { NodeId = nodeId, NodeName = nodeName };
                _metrics[nodeId] = m;
            }

            m.MessagesProcessed++;
            m.BytesProcessed += bytes;
            m.LatencySamples.Add(latencyMs);
            var now = DateTime.UtcNow;
            if (now < m.FirstMessageTime) m.FirstMessageTime = now;
            if (now > m.LastMessageTime) m.LastMessageTime = now;
        }
    }

    public void RecordDropped(string nodeId, string nodeName)
    {
        lock (_lock)
        {
            if (!_metrics.TryGetValue(nodeId, out var m))
            {
                m = new NodeMetrics { NodeId = nodeId, NodeName = nodeName };
                _metrics[nodeId] = m;
            }
            m.MessagesDropped++;
        }
    }

    public void RecordError(string nodeId, string nodeName, string errorType)
    {
        lock (_lock)
        {
            if (!_metrics.TryGetValue(nodeId, out var m))
            {
                m = new NodeMetrics { NodeId = nodeId, NodeName = nodeName };
                _metrics[nodeId] = m;
            }
            m.ErrorCount++;
        }
    }

    public Dictionary<string, NodeMetrics> GetAllMetrics()
    {
        lock (_lock)
        {
            return new Dictionary<string, NodeMetrics>(_metrics);
        }
    }
}
```

## Step 3: Implement Health Calculator

Calculate health status from metrics:

```csharp
public static class HealthCalculator
{
    public static string GetNodeStatus(NodeMetrics metrics)
    {
        var errorRate = metrics.MessagesProcessed > 0
            ? (double)metrics.ErrorCount / metrics.MessagesProcessed * 100
            : 0;

        var p99 = GetPercentile(metrics.LatencySamples, 99);

        if (errorRate > 5.0 || p99 > 1000) return "CRIT";
        if (errorRate > 1.0 || p99 > 500) return "WARN";
        if (metrics.ErrorCount > 0) return "WARN";
        return "OK";
    }

    public static double GetPercentile(List<double> values, int percentile)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }

    public static double GetThroughput(NodeMetrics metrics)
    {
        if (metrics.FirstMessageTime == DateTime.MaxValue) return 0;
        var duration = metrics.LastMessageTime - metrics.FirstMessageTime;
        if (duration.TotalSeconds < 0.001) return metrics.MessagesProcessed;
        return metrics.MessagesProcessed / duration.TotalSeconds;
    }
}
```

## Step 4: Implement Message Tracer

Create a tracer that follows messages through the pipeline:

```csharp
public class MessageTracer
{
    private readonly Dictionary<string, List<TraceEntry>> _traces = new();

    public string StartTrace(string message)
    {
        var traceId = Guid.NewGuid().ToString("N")[..8];
        _traces[traceId] = new List<TraceEntry>();
        return traceId;
    }

    public void AddEntry(string traceId, string nodeId, string nodeName,
        string action, string data, double latencyMs)
    {
        if (_traces.TryGetValue(traceId, out var entries))
        {
            entries.Add(new TraceEntry(
                Timestamp: DateTime.UtcNow,
                NodeId: nodeId,
                NodeName: nodeName,
                Action: action,
                Data: data.Length > 100 ? data[..100] + "..." : data,
                LatencyMs: latencyMs
            ));
        }
    }

    public List<TraceEntry> GetTrace(string traceId)
    {
        return _traces.TryGetValue(traceId, out var entries)
            ? entries
            : new List<TraceEntry>();
    }
}

public record TraceEntry(
    DateTime Timestamp,
    string NodeId,
    string NodeName,
    string Action,
    string Data,
    double LatencyMs
);
```

## Step 5: Build the Dashboard Display

Create an ASCII dashboard:

```csharp
public static class PipelineDashboard
{
    public static void Display(Dictionary<string, NodeMetrics> metrics)
    {
        Console.WriteLine("+-----------------------------------------------+");
        Console.WriteLine("|          Pipeline Monitoring Dashboard         |");
        Console.WriteLine("+-----------------------------------------------+");
        Console.WriteLine();

        foreach (var (nodeId, m) in metrics.OrderBy(kvp => kvp.Key))
        {
            var status = HealthCalculator.GetNodeStatus(m);
            var statusIcon = status switch
            {
                "OK" => "[OK]  ",
                "WARN" => "[WARN]",
                "CRIT" => "[CRIT]",
                _ => "[ERR] "
            };

            var throughput = HealthCalculator.GetThroughput(m);
            var p50 = HealthCalculator.GetPercentile(m.LatencySamples, 50);
            var p99 = HealthCalculator.GetPercentile(m.LatencySamples, 99);

            Console.WriteLine($"  {statusIcon} {m.NodeName,-25} | Processed: {m.MessagesProcessed,6} | Dropped: {m.MessagesDropped,4} | Errors: {m.ErrorCount,3}");
            Console.WriteLine($"         {"",-25} | Throughput: {throughput,6:F0} msg/s | P50: {p50,6:F1}ms | P99: {p99,6:F1}ms");
            Console.WriteLine();
        }
    }
}
```

## Step 6: Simulate Pipeline with Monitoring

Run a pipeline and collect metrics:

```csharp
// Create a monitored pipeline
var collector = new PipelineMetricsCollector();
var tracer = new MessageTracer();

// Process messages through a simulated pipeline
for (int i = 0; i < 100; i++)
{
    var message = $"{{\"orderId\":\"ORD-{i:D4}\",\"amount\":{Random.Shared.Next(10, 500)}}}";
    var traceId = tracer.StartTrace(message);

    // Source node
    var sw = Stopwatch.StartNew();
    // ... process ...
    sw.Stop();
    collector.RecordProcessed("source", "Order Source", message.Length, sw.Elapsed.TotalMilliseconds);
    tracer.AddEntry(traceId, "source", "Order Source", "received", message, sw.Elapsed.TotalMilliseconds);

    // Transform node
    sw.Restart();
    // ... transform ...
    sw.Stop();
    collector.RecordProcessed("transform", "Validator", message.Length, sw.Elapsed.TotalMilliseconds);

    // Sink node
    sw.Restart();
    // ... produce to output ...
    sw.Stop();
    collector.RecordProcessed("sink", "Output Sink", message.Length, sw.Elapsed.TotalMilliseconds);
}

// Display dashboard
PipelineDashboard.Display(collector.GetAllMetrics());
```

## Complete Solution

See `completed/Program.cs` for the full implementation.

## Sample Output

```
=== Surgewave Bootcamp: Lesson 25.5 - Pipeline Monitoring and Debugging ===

Surgewave broker started on port 12345

--- Processing 100 messages through monitored pipeline ---
[source] Received ORD-0001
[transform] Validated ORD-0001 (2.1ms)
[sink] Produced ORD-0001 to output-orders (1.5ms)
...

+-----------------------------------------------+
|          Pipeline Monitoring Dashboard         |
+-----------------------------------------------+

  [OK]   Order Source               | Processed:    100 | Dropped:    0 | Errors:   0
                                    | Throughput:   1250 msg/s | P50:    0.8ms | P99:    3.2ms

  [OK]   Order Validator            | Processed:     95 | Dropped:    5 | Errors:   0
                                    | Throughput:   1190 msg/s | P50:    1.2ms | P99:    5.1ms

  [WARN] External Enrichment        | Processed:     93 | Dropped:    0 | Errors:   2
                                    | Throughput:    850 msg/s | P50:   12.5ms | P99:   95.3ms

  [OK]   Output Sink                | Processed:     93 | Dropped:    0 | Errors:   0
                                    | Throughput:   1100 msg/s | P50:    1.0ms | P99:    4.2ms

--- Message Trace (ORD-0050) ---
  [0.0ms] Order Source: received {"orderId":"ORD-0050","amount":250}
  [0.8ms] Order Validator: validated (passed)
  [12.3ms] External Enrichment: enriched (added customer data)
  [13.5ms] Output Sink: produced to output-orders

--- Pipeline Issues Detected ---
  [WARN] External Enrichment: 2 errors detected (2.1% error rate)
  [INFO] Bottleneck: External Enrichment (lowest throughput: 850 msg/s)

Lesson complete!
```

## Knowledge Check

1. What are the key metrics to monitor in a data pipeline?
2. How do you identify bottlenecks using metrics?
3. What is message tracing and when is it useful?
4. How should pipeline alerts be configured?
5. What is the difference between P50, P90, and P99 latency?

---

**Next:** [Unit 16 Project: Visual Pipeline Project](../project/README.md)
