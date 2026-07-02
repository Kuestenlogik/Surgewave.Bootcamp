// Unit 9 Project: Observability Stack
// Build a fully observable Surgewave application

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client.Native;
using Kuestenlogik.Surgewave.Broker;

Console.WriteLine("=== Observability Stack: Payment Processing ===\n");

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

    public void LogInfo(string message)
    {
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
    private readonly ConcurrentBag<double> _latencies = new();

    public void IncrementCounter(string name, long value = 1)
    {
        _counters.AddOrUpdate(name, value, (_, current) => current + value);
        Console.WriteLine($"[Metric] {name} +{value}");
    }

    public void RecordLatency(double milliseconds)
    {
        _latencies.Add(milliseconds);
        Console.WriteLine($"[Metric] payments.latency.ms {milliseconds:F0}");
    }

    public long GetCounter(string name) => _counters.GetValueOrDefault(name, 0);
    public double GetAverageLatency() => _latencies.Any() ? _latencies.Average() : 0;
    public double GetP99Latency() => _latencies.Any()
        ? _latencies.OrderBy(x => x).ElementAt((int)(_latencies.Count * 0.99))
        : 0;
}

// Initialize observability components
var logger = new SimpleLogger();
var metrics = new MetricsCollector();
var spanCount = 0;

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
await using var surgewave = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .Build()
    .StartAsync();

// Create native client for fluent API
await using var client = new SurgewaveNativeClient("localhost", surgewave.Port);
await client.ConnectAsync();

// TODO: Create "payments" topic
logger.LogInfo("Payment system initialized");

// TODO: Start a root Activity for distributed tracing
// Hint: var source = new ActivitySource("PaymentPipeline");
//       using var rootActivity = source.StartActivity("payment-pipeline");
Console.WriteLine("[Trace] Starting trace: payment-pipeline\n");

// Step 1: Produce payment requests with tracing
Console.WriteLine("[Producer] Processing payment requests...");

foreach (var payment in payments)
{
    // TODO: Create a child span for producing
    // TODO: Log the payment with structured data
    // TODO: Increment "payments.requested" counter
    // TODO: Produce the payment to Surgewave
    // TODO: Include trace context in message headers

    logger.LogInfo($"PaymentId={payment.PaymentId}, CustomerId={payment.CustomerId}, Amount={payment.Amount:F2} {payment.Currency}");

    // TODO: Produce to "payments" topic
    // Hint: Store TraceId in message headers for propagation
}

Console.WriteLine();

// Step 2: Consume and process payments
Console.WriteLine("[Consumer] Processing payments...");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var processedCount = 0;

// TODO: Consume from "payments" topic
// For each message:
//   1. Extract trace context from headers
//   2. Create a child span for processing
//   3. Deserialize the payment
//   4. Simulate processing with a delay
//   5. Record latency metric
//   6. Increment processed counter and amount

// TODO: Implement consumer with observability here

Console.WriteLine();

// Step 3: Display observability summary
Console.WriteLine("=== Observability Summary ===");

Console.WriteLine("[Metrics]");
// TODO: Display metrics summary
Console.WriteLine($"  - Payments Requested: {metrics.GetCounter("payments.requested")}");
Console.WriteLine($"  - Payments Processed: {metrics.GetCounter("payments.processed")}");
// TODO: Add total amount and latency statistics

Console.WriteLine("\n[Tracing]");
// TODO: Display tracing summary
Console.WriteLine($"  - Total Spans: {spanCount}");
Console.WriteLine("  - Root Trace: payment-pipeline");

Console.WriteLine("\n[Logging]");
Console.WriteLine($"  - Info: {logger.GetCount("Info")} entries");
Console.WriteLine($"  - Warning: {logger.GetCount("Warning")} entries");
Console.WriteLine($"  - Error: {logger.GetCount("Error")} entries");

Console.WriteLine("\n=== Observability Stack Complete ===");