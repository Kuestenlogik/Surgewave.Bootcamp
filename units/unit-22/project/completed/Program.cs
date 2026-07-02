// Unit 9 Project: Observability Stack - COMPLETED
// A fully observable Surgewave payment processing application

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;
using Kuestenlogik.Surgewave.Client.Producer;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Observability Stack: Payment Processing ===\n");

// Initialize observability components
var logger = new SimpleLogger();
var metrics = new MetricsCollector();
var spanCount = 0;

// Create ActivitySource for distributed tracing
var activitySource = new ActivitySource("PaymentPipeline", "1.0.0");

// Sample payment requests
var payments = new List<PaymentRequest>
{
    new("PAY-001", "C-100", 150.00m, "USD", DateTime.UtcNow),
    new("PAY-002", "C-101", 2500.00m, "USD", DateTime.UtcNow),
    new("PAY-003", "C-102", 75.50m, "USD", DateTime.UtcNow),
    new("PAY-004", "C-100", 899.99m, "USD", DateTime.UtcNow),
    new("PAY-005", "C-103", 624.51m, "USD", DateTime.UtcNow),
};

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

var bootstrapServers = runtime.BootstrapServers;

// Create producer and consumer
await using var producer = new SurgewaveProducer<string, string>(opt => opt.BootstrapServers = bootstrapServers);
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = bootstrapServers;
    opt.GroupId = "payment-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

logger.LogInfo("Payment system initialized");

// Start root activity for the pipeline
using var rootActivity = activitySource.StartActivity("payment-pipeline", ActivityKind.Internal);
spanCount++;

var traceId = rootActivity?.TraceId.ToString() ?? Guid.NewGuid().ToString("N")[..12];
Console.WriteLine($"[Trace] Starting trace: payment-pipeline (TraceId: {traceId})\n");

// Step 1: Produce payment requests with tracing
Console.WriteLine("[Producer] Processing payment requests...");

foreach (var payment in payments)
{
    // Create child span for producing
    using var produceActivity = activitySource.StartActivity("produce-payment", ActivityKind.Producer, rootActivity?.Context ?? default);
    spanCount++;

    produceActivity?.SetTag("payment.id", payment.PaymentId);
    produceActivity?.SetTag("payment.amount", payment.Amount);
    produceActivity?.SetTag("payment.currency", payment.Currency);

    // Log with structured data
    logger.LogInfo($"PaymentId={payment.PaymentId}, CustomerId={payment.CustomerId}, Amount={payment.Amount:F2} {payment.Currency}");

    // Increment request counter
    metrics.IncrementCounter("payments.requested");

    // Prepare message with trace context in headers
    var headers = new Dictionary<string, byte[]>
    {
        { "traceparent", Encoding.UTF8.GetBytes(produceActivity?.Id ?? "") },
        { "tracestate", Encoding.UTF8.GetBytes(produceActivity?.TraceStateString ?? "") }
    };

    var json = JsonSerializer.Serialize(payment);

    // Produce to Surgewave
    var result = await producer.ProduceAsync("payments", payment.CustomerId, json, headers);

    Console.WriteLine($"[Trace] Span: produce-payment (SpanId: {produceActivity?.SpanId.ToString()?[..8]})\n");
}

// Step 2: Consume and process payments
Console.WriteLine("[Consumer] Processing payments...");

consumer.Subscribe("payments");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var processedCount = 0;
var random = new Random();

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var message = await consumer.ConsumeAsync(cts.Token);
        if (message is null) continue;

        var stopwatch = Stopwatch.StartNew();

        // Extract trace context from headers
        var traceparentBytes = message.Headers?.GetValueOrDefault("traceparent");
        var traceparent = traceparentBytes != null ? Encoding.UTF8.GetString(traceparentBytes) : "";

        // Create child span for processing
        ActivityContext parentContext = default;
        if (ActivityContext.TryParse(traceparent, null, out var parsed))
        {
            parentContext = parsed;
        }

        using var processActivity = activitySource.StartActivity("process-payment", ActivityKind.Consumer, parentContext);
        spanCount++;

        // Deserialize payment
        var payment = JsonSerializer.Deserialize<PaymentRequest>(message.Value);
        if (payment is null) continue;

        processActivity?.SetTag("payment.id", payment.PaymentId);

        // Log processing with trace context
        logger.LogInfo($"Processing {payment.PaymentId} (TraceId: {traceId}, SpanId: {processActivity?.SpanId.ToString()?[..8]})");

        // Simulate processing time (20-80ms)
        await Task.Delay(random.Next(20, 80));

        stopwatch.Stop();

        // Record metrics
        metrics.IncrementCounter("payments.processed");
        metrics.AddAmount("payments.amount.total", payment.Amount);
        metrics.RecordLatency(stopwatch.Elapsed.TotalMilliseconds);

        processedCount++;

        // Simulate occasional warning
        if (payment.Amount > 2000)
        {
            logger.LogWarning($"High-value payment detected: {payment.PaymentId} (${payment.Amount:F2})");
        }

        Console.WriteLine();

        // Stop after processing all
        if (processedCount >= payments.Count) break;
    }
}
catch (OperationCanceledException)
{
    // Expected timeout
}

// Step 3: Display observability summary
Console.WriteLine("=== Observability Summary ===");

Console.WriteLine("[Metrics]");
Console.WriteLine($"  - Payments Requested: {metrics.GetCounter("payments.requested")}");
Console.WriteLine($"  - Payments Processed: {metrics.GetCounter("payments.processed")}");
Console.WriteLine($"  - Total Amount: ${metrics.GetAmount("payments.amount.total"):N2}");
Console.WriteLine($"  - Avg Latency: {metrics.GetAverageLatency():F0}ms");
Console.WriteLine($"  - P99 Latency: {metrics.GetP99Latency():F0}ms");

Console.WriteLine("\n[Tracing]");
Console.WriteLine($"  - Total Spans: {spanCount}");
Console.WriteLine($"  - Root Trace: payment-pipeline ({traceId})");

Console.WriteLine("\n[Logging]");
Console.WriteLine($"  - Info: {logger.GetCount("Info")} entries");
Console.WriteLine($"  - Warning: {logger.GetCount("Warning")} entries");
Console.WriteLine($"  - Error: {logger.GetCount("Error")} entries");

Console.WriteLine("\n=== Observability Stack Complete ===");

// Data models
public record PaymentRequest(
    string PaymentId,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateTime RequestedAt
);

public record PaymentResult(
    string PaymentId,
    bool Success,
    string? ErrorMessage,
    TimeSpan ProcessingTime
);

// Simple logger that tracks log entries
public class SimpleLogger
{
    private readonly ConcurrentBag<(string Level, string Message, DateTime Timestamp)> _entries = new();

    public void LogInfo(string message, params object[] args)
    {
        var formattedMessage = string.Format(message.Replace("{", "{{").Replace("}}", "}}"), args);
        _entries.Add(("Info", message, DateTime.UtcNow));
        Console.WriteLine($"[Logger] {message}");
    }

    public void LogWarning(string message)
    {
        _entries.Add(("Warning", message, DateTime.UtcNow));
        Console.WriteLine($"[Logger] WARN: {message}");
    }

    public void LogError(string message)
    {
        _entries.Add(("Error", message, DateTime.UtcNow));
        Console.WriteLine($"[Logger] ERROR: {message}");
    }

    public int GetCount(string level) => _entries.Count(e => e.Level == level);
}

// Simple metrics collector
public class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, decimal> _amounts = new();
    private readonly ConcurrentBag<double> _latencies = new();

    public void IncrementCounter(string name, long value = 1)
    {
        _counters.AddOrUpdate(name, value, (_, current) => current + value);
        Console.WriteLine($"[Metric] {name} +{value}");
    }

    public void AddAmount(string name, decimal value)
    {
        _amounts.AddOrUpdate(name, value, (_, current) => current + value);
        Console.WriteLine($"[Metric] {name} +{value:F2}");
    }

    public void RecordLatency(double milliseconds)
    {
        _latencies.Add(milliseconds);
        Console.WriteLine($"[Metric] payments.latency.ms {milliseconds:F0}");
    }

    public long GetCounter(string name) => _counters.GetValueOrDefault(name, 0);
    public decimal GetAmount(string name) => _amounts.GetValueOrDefault(name, 0);
    public double GetAverageLatency() => _latencies.Any() ? _latencies.Average() : 0;
    public double GetP99Latency()
    {
        if (!_latencies.Any()) return 0;
        var sorted = _latencies.OrderBy(x => x).ToList();
        var index = Math.Min((int)(sorted.Count * 0.99), sorted.Count - 1);
        return sorted[index];
    }
}