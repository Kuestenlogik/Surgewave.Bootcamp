// Surgewave Bootcamp - Unit 25 Project: Visual Pipeline - Multi-Stage Data Processing
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Visual Pipeline Project: E-Commerce Order Processing ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;
Console.WriteLine($"[Setup] Surgewave broker started on port {runtime.Port}");
Console.WriteLine("[Setup] Pipeline: Source -> Filter -> Enrich -> Router -> Sinks");

// Create monitoring infrastructure
var collector = new PipelineMetricsCollector();

// ============================================================================
// Phase 1: Produce test orders
// ============================================================================
Console.WriteLine("\n--- Phase 1: Producing Test Orders ---");

await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);

var orderCount = 200;
var customers = new[] { "Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace" };
var priorities = new[] { "high", "high", "standard", "standard", "standard", "standard" }; // ~33% high

for (int i = 0; i < orderCount; i++)
{
    var amount = Random.Shared.Next(0, 500); // 0 = invalid
    var customer = customers[Random.Shared.Next(customers.Length)];
    var priority = priorities[Random.Shared.Next(priorities.Length)];

    var order = JsonSerializer.Serialize(new
    {
        orderId = $"ORD-{i:D4}",
        customer,
        amount,
        priority,
        timestamp = DateTime.UtcNow.ToString("O")
    });

    await producer.ProduceAsync("raw-orders", $"ORD-{i:D4}", order);
}

Console.WriteLine($"[Producer] Sent {orderCount} orders (mix of valid/invalid, high/standard priority)");

// ============================================================================
// Phase 2: Process pipeline
// ============================================================================
Console.WriteLine("\n--- Phase 2: Processing Pipeline ---");

await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "pipeline-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("raw-orders");

var prioritySinkCount = 0;
var standardSinkCount = 0;
var totalProcessed = 0;
var overallSw = Stopwatch.StartNew();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    while (!cts.Token.IsCancellationRequested && totalProcessed < orderCount)
    {
        var record = await consumer.ConsumeAsync(cts.Token);
        if (record?.Value is null) continue;

        totalProcessed++;
        var message = record.Value;

        // --- Source Node ---
        var sw = Stopwatch.StartNew();
        sw.Stop();
        collector.RecordProcessed("source", "Order Source", message.Length, sw.Elapsed.TotalMilliseconds);

        // --- Filter Node: Validate order ---
        sw.Restart();
        var isValid = !message.Contains("\"amount\":0");
        sw.Stop();

        if (!isValid)
        {
            collector.RecordDropped("filter", "Order Validator");
            if (totalProcessed <= 5 || totalProcessed % 50 == 0)
                Console.WriteLine($"  Processing order ORD-{totalProcessed - 1:D4}... [invalid - amount 0] [dropped]");
            continue;
        }

        collector.RecordProcessed("filter", "Order Validator", message.Length, sw.Elapsed.TotalMilliseconds);

        // --- Enrich Node: Add customer data ---
        sw.Restart();
        var enrichDelay = Random.Shared.Next(1, 10);
        await Task.Delay(enrichDelay); // Simulate enrichment latency

        // Simulate occasional enrichment errors
        var hasError = Random.Shared.Next(0, 100) < 2;
        sw.Stop();

        if (hasError)
        {
            collector.RecordError("enrich", "Customer Enrichment", "lookup_failed");
            continue;
        }

        // Parse and re-serialize with enrichment
        using var doc = JsonDocument.Parse(message);
        var enrichedOrder = JsonSerializer.Serialize(new
        {
            orderId = doc.RootElement.GetProperty("orderId").GetString(),
            customer = doc.RootElement.GetProperty("customer").GetString(),
            amount = doc.RootElement.GetProperty("amount").GetInt32(),
            priority = doc.RootElement.GetProperty("priority").GetString(),
            enriched = true,
            loyaltyTier = Random.Shared.Next(1, 4) switch { 1 => "gold", 2 => "silver", _ => "bronze" }
        });

        collector.RecordProcessed("enrich", "Customer Enrichment", enrichedOrder.Length, sw.Elapsed.TotalMilliseconds);

        // --- Router Node: Route by priority ---
        sw.Restart();
        var isHighPriority = message.Contains("\"priority\":\"high\"");
        sw.Stop();
        collector.RecordProcessed("router", "Priority Router", enrichedOrder.Length, sw.Elapsed.TotalMilliseconds);

        // --- Sink Nodes ---
        sw.Restart();
        if (isHighPriority)
        {
            await producer.ProduceAsync("priority-orders", record.Key ?? "", enrichedOrder);
            prioritySinkCount++;
            collector.RecordProcessed("sink-priority", "Priority Sink", enrichedOrder.Length, sw.Elapsed.TotalMilliseconds);
        }
        else
        {
            await producer.ProduceAsync("standard-orders", record.Key ?? "", enrichedOrder);
            standardSinkCount++;
            collector.RecordProcessed("sink-standard", "Standard Sink", enrichedOrder.Length, sw.Elapsed.TotalMilliseconds);
        }
        sw.Stop();

        var priority = isHighPriority ? "high" : "standard";
        if (totalProcessed <= 5 || totalProcessed % 50 == 0)
            Console.WriteLine($"  Processing order ORD-{totalProcessed - 1:D4}... [valid] [enriched] [priority: {priority}]");
    }
}
catch (OperationCanceledException)
{
    // Expected
}

overallSw.Stop();
Console.WriteLine($"  Processed {totalProcessed} messages in {overallSw.Elapsed.TotalSeconds:F1} seconds");

// ============================================================================
// Phase 3: Display dashboard
// ============================================================================
Console.WriteLine("\n--- Phase 3: Pipeline Dashboard ---");
DisplayDashboard(collector.GetAllMetrics());

// ============================================================================
// Phase 4: Analysis
// ============================================================================
Console.WriteLine("--- Phase 4: Analysis ---");

var allMetrics = collector.GetAllMetrics();

// Find bottleneck
string? bottleneckId = null;
double lowestThroughput = double.MaxValue;
foreach (var (nodeId, m) in allMetrics)
{
    if (m.MessagesProcessed == 0) continue;
    var throughput = GetThroughput(m);
    if (throughput < lowestThroughput)
    {
        lowestThroughput = throughput;
        bottleneckId = nodeId;
    }
}
if (bottleneckId is not null)
    Console.WriteLine($"  Bottleneck: {allMetrics[bottleneckId].NodeName} ({lowestThroughput:F0} msg/s)");

// Drop rate
if (allMetrics.TryGetValue("filter", out var filterMetrics))
{
    var total = filterMetrics.MessagesProcessed + filterMetrics.MessagesDropped;
    var dropRate = total > 0 ? (double)filterMetrics.MessagesDropped / total * 100 : 0;
    Console.WriteLine($"  Drop rate: {dropRate:F1}% at {filterMetrics.NodeName}");
}

// Error rate
if (allMetrics.TryGetValue("enrich", out var enrichMetrics))
{
    var total = enrichMetrics.MessagesProcessed + enrichMetrics.ErrorCount;
    var errorRate = total > 0 ? (double)enrichMetrics.ErrorCount / total * 100 : 0;
    Console.WriteLine($"  Error rate: {errorRate:F1}% at {enrichMetrics.NodeName}");
}

// Priority split
var totalOutput = prioritySinkCount + standardSinkCount;
if (totalOutput > 0)
{
    Console.WriteLine($"  High-priority orders: {prioritySinkCount} ({(double)prioritySinkCount / totalOutput * 100:F1}%)");
    Console.WriteLine($"  Standard orders: {standardSinkCount} ({(double)standardSinkCount / totalOutput * 100:F1}%)");
}

Console.WriteLine();
Console.WriteLine("=== Pipeline Complete ===");

// ============================================================================
// Helper methods
// ============================================================================

static void DisplayDashboard(Dictionary<string, NodeMetrics> metrics)
{
    Console.WriteLine("+----------------------------------------------------------+");
    Console.WriteLine("|              Pipeline Monitoring Dashboard                |");
    Console.WriteLine("+----------------------------------------------------------+");
    Console.WriteLine();

    foreach (var (nodeId, m) in metrics.OrderBy(kvp => kvp.Key))
    {
        var status = GetNodeStatus(m);
        var statusIcon = status switch
        {
            "OK" => "[OK]  ",
            "WARN" => "[WARN]",
            "CRIT" => "[CRIT]",
            _ => "[ERR] "
        };

        var throughput = GetThroughput(m);
        var p50 = GetPercentile(m.LatencySamples, 50);
        var p99 = GetPercentile(m.LatencySamples, 99);

        Console.WriteLine($"  {statusIcon} {m.NodeName,-25} | Processed: {m.MessagesProcessed,6} | Dropped: {m.MessagesDropped,4} | Errors: {m.ErrorCount,3}");
        Console.WriteLine($"         {"",-25} | Throughput: {throughput,6:F0} msg/s | P50: {p50,6:F1}ms | P99: {p99,6:F1}ms");
        Console.WriteLine();
    }
}

static string GetNodeStatus(NodeMetrics metrics)
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

static double GetPercentile(List<double> values, int percentile)
{
    if (values.Count == 0) return 0;
    var sorted = values.OrderBy(v => v).ToList();
    var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
    return sorted[Math.Max(0, index)];
}

static double GetThroughput(NodeMetrics metrics)
{
    if (metrics.FirstMessageTime == DateTime.MaxValue) return 0;
    var duration = metrics.LastMessageTime - metrics.FirstMessageTime;
    if (duration.TotalSeconds < 0.001) return metrics.MessagesProcessed;
    return metrics.MessagesProcessed / duration.TotalSeconds;
}

// ============================================================================
// Types
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