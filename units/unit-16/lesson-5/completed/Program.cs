// Surgewave Bootcamp - Unit 25, Lesson 5: Pipeline Monitoring and Debugging
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 25.5 - Pipeline Monitoring and Debugging ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");
var bootstrapServers = runtime.BootstrapServers;

// Create monitoring infrastructure
var collector = new PipelineMetricsCollector();
var tracer = new MessageTracer();

// ============================================================================
// Process messages through a monitored pipeline
// ============================================================================
Console.WriteLine("\n--- Processing 100 messages through monitored pipeline ---");

await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

string? sampleTraceId = null;

for (int i = 0; i < 100; i++)
{
    var amount = Random.Shared.Next(0, 500); // Some will be 0 (invalid)
    var message = $"{{\"orderId\":\"ORD-{i:D4}\",\"amount\":{amount}}}";
    var traceId = tracer.StartTrace(message);

    // Keep a trace for display
    if (i == 50) sampleTraceId = traceId;

    // Source node: receive message
    var sw = Stopwatch.StartNew();
    await producer.ProduceAsync("input-orders", $"key-{i}", message);
    sw.Stop();
    collector.RecordProcessed("source", "Order Source", message.Length, sw.Elapsed.TotalMilliseconds);
    tracer.AddEntry(traceId, "source", "Order Source", "received", message, sw.Elapsed.TotalMilliseconds);

    // Transform node: validate order
    sw.Restart();
    var isValid = amount > 0;
    // Simulate occasional slow processing
    if (i % 20 == 0) await Task.Delay(5);
    sw.Stop();

    if (isValid)
    {
        collector.RecordProcessed("transform", "Order Validator", message.Length, sw.Elapsed.TotalMilliseconds);
        tracer.AddEntry(traceId, "transform", "Order Validator", "validated (passed)", message, sw.Elapsed.TotalMilliseconds);
    }
    else
    {
        collector.RecordDropped("transform", "Order Validator");
        tracer.AddEntry(traceId, "transform", "Order Validator", "dropped (invalid amount)", message, sw.Elapsed.TotalMilliseconds);
        continue; // Skip to next message
    }

    // Enrichment node: simulate external call with variable latency
    sw.Restart();
    var enrichDelay = Random.Shared.Next(1, 15);
    await Task.Delay(enrichDelay);
    // Simulate occasional errors
    var hasError = Random.Shared.Next(0, 100) < 3; // 3% error rate
    sw.Stop();

    if (hasError)
    {
        collector.RecordError("enrichment", "External Enrichment", "timeout");
        tracer.AddEntry(traceId, "enrichment", "External Enrichment", "ERROR: timeout", message, sw.Elapsed.TotalMilliseconds);
        continue;
    }

    var enrichedMessage = $"{{\"orderId\":\"ORD-{i:D4}\",\"amount\":{amount},\"enriched\":true}}";
    collector.RecordProcessed("enrichment", "External Enrichment", enrichedMessage.Length, sw.Elapsed.TotalMilliseconds);
    tracer.AddEntry(traceId, "enrichment", "External Enrichment", "enriched", enrichedMessage, sw.Elapsed.TotalMilliseconds);

    // Sink node: produce to output
    sw.Restart();
    await producer.ProduceAsync("output-orders", $"key-{i}", enrichedMessage);
    sw.Stop();
    collector.RecordProcessed("sink", "Output Sink", enrichedMessage.Length, sw.Elapsed.TotalMilliseconds);
    tracer.AddEntry(traceId, "sink", "Output Sink", "produced to output-orders", enrichedMessage, sw.Elapsed.TotalMilliseconds);

    // Log progress every 25 messages
    if ((i + 1) % 25 == 0)
    {
        Console.WriteLine($"  Processed {i + 1}/100 messages...");
    }
}

// ============================================================================
// Display monitoring dashboard
// ============================================================================
Console.WriteLine();
PipelineDashboard.Display(collector.GetAllMetrics());

// ============================================================================
// Show a message trace
// ============================================================================
if (sampleTraceId is not null)
{
    Console.WriteLine("--- Message Trace (ORD-0050) ---");
    var trace = tracer.GetTrace(sampleTraceId);
    var baseTime = trace.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow;

    foreach (var entry in trace)
    {
        var elapsed = (entry.Timestamp - baseTime).TotalMilliseconds;
        Console.WriteLine($"  [{elapsed,6:F1}ms] {entry.NodeName}: {entry.Action}");
    }
    Console.WriteLine();
}

// ============================================================================
// Detect pipeline issues
// ============================================================================
Console.WriteLine("--- Pipeline Issues Detected ---");

var allMetrics = collector.GetAllMetrics();

// Check for nodes with errors
foreach (var (nodeId, m) in allMetrics.Where(kvp => kvp.Value.ErrorCount > 0))
{
    var errorRate = m.MessagesProcessed > 0
        ? (double)m.ErrorCount / (m.MessagesProcessed + m.ErrorCount) * 100
        : 100;
    var status = HealthCalculator.GetNodeStatus(m);
    Console.WriteLine($"  [{status}] {m.NodeName}: {m.ErrorCount} errors detected ({errorRate:F1}% error rate)");
}

// Find bottleneck
var bottleneckId = FindBottleneck(allMetrics);
if (bottleneckId is not null && allMetrics.TryGetValue(bottleneckId, out var bottleneckMetrics))
{
    var throughput = HealthCalculator.GetThroughput(bottleneckMetrics);
    Console.WriteLine($"  [INFO] Bottleneck: {bottleneckMetrics.NodeName} (lowest throughput: {throughput:F0} msg/s)");
}

// Check for high drop rate
foreach (var (nodeId, m) in allMetrics.Where(kvp => kvp.Value.MessagesDropped > 0))
{
    var total = m.MessagesProcessed + m.MessagesDropped;
    var dropRate = total > 0 ? (double)m.MessagesDropped / total * 100 : 0;
    Console.WriteLine($"  [INFO] {m.NodeName}: {m.MessagesDropped} messages dropped ({dropRate:F1}% drop rate)");
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Monitor throughput, latency (P50/P90/P99), and error rate per node");
Console.WriteLine("2. Message tracing helps debug data transformation issues");
Console.WriteLine("3. The bottleneck is the node with the lowest throughput");
Console.WriteLine("4. Health status should be graduated (OK -> WARN -> CRIT)");
Console.WriteLine("5. Continuous monitoring establishes baselines for anomaly detection");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

static string? FindBottleneck(Dictionary<string, NodeMetrics> metrics)
{
    if (metrics.Count == 0) return null;

    string? bottleneckId = null;
    double lowestThroughput = double.MaxValue;

    foreach (var (nodeId, m) in metrics)
    {
        if (m.MessagesProcessed == 0) continue;

        var throughput = HealthCalculator.GetThroughput(m);

        if (throughput < lowestThroughput)
        {
            lowestThroughput = throughput;
            bottleneckId = nodeId;
        }
    }

    return bottleneckId;
}

// ============================================================================
// Metrics Types
// ============================================================================

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

public record TraceEntry(
    DateTime Timestamp,
    string NodeId,
    string NodeName,
    string Action,
    string Data,
    double LatencyMs
);

// ============================================================================
// Metrics Collector
// ============================================================================

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

// ============================================================================
// Health Calculator
// ============================================================================

public static class HealthCalculator
{
    public static string GetNodeStatus(NodeMetrics metrics)
    {
        var total = metrics.MessagesProcessed + metrics.ErrorCount;
        var errorRate = total > 0
            ? (double)metrics.ErrorCount / total * 100
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

// ============================================================================
// Message Tracer
// ============================================================================

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

// ============================================================================
// Dashboard
// ============================================================================

public static class PipelineDashboard
{
    public static void Display(Dictionary<string, NodeMetrics> metrics)
    {
        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine("|              Pipeline Monitoring Dashboard                |");
        Console.WriteLine("+----------------------------------------------------------+");
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
