// Surgewave Bootcamp - Unit 11, Lesson 3: Log Analytics
//
// In this lesson, you'll build a centralized logging platform with:
// - Log aggregation from multiple services
// - Log parsing and enrichment
// - Error filtering and alerting
// - Real-time dashboards
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.3 - Log Analytics ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Configuration ==============
var services = new[] { "api-gateway", "user-service", "order-service", "payment-service" };
var logMessages = new Dictionary<string, string[]>
{
    ["INFO"] = new[]
    {
        "Request received",
        "Processing completed",
        "Cache hit",
        "Connection established",
        "Health check passed"
    },
    ["WARN"] = new[]
    {
        "Slow query detected",
        "Retry attempt",
        "Cache miss",
        "Connection pool exhausted",
        "Rate limit approaching"
    },
    ["ERROR"] = new[]
    {
        "Request failed",
        "Database connection lost",
        "Authentication failed",
        "Timeout exceeded",
        "Invalid payload"
    }
};

// ============== Step 1: Log Producers (Service Simulators) ==============
Console.WriteLine("--- Step 1: Generating Logs ---");

// TODO: Create a producer for raw-logs topic
// Hint: await using var logProducer = new SurgewaveProducer<byte[], byte[]>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });

// TODO: Generate logs from multiple services
// Hint: var random = new Random(42);
// var levels = new[] { "INFO", "INFO", "INFO", "INFO", "WARN", "ERROR" };
// for (var i = 0; i < 50; i++)
// {
//     var service = services[random.Next(services.Length)];
//     var level = levels[random.Next(levels.Length)];
//     var message = logMessages[level][random.Next(logMessages[level].Length)];
//     var rawLog = new RawLogEntry(service, $"[{level}] {message}", DateTime.UtcNow);
//     await logProducer.ProduceAsync("raw-logs",
//         Encoding.UTF8.GetBytes(service),
//         Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rawLog)));
// }

Console.WriteLine("Logs generated!");
Console.WriteLine();

// ============== Step 2: Log Processor ==============
Console.WriteLine("--- Step 2: Processing Logs ---");

// TODO: Create a consumer for raw-logs topic
// TODO: Create producers for parsed-logs and error-logs topics

// TODO: Parse and enrich each log entry
// Hint: var levelPattern = new Regex(@"^\[(\w+)\]\s*(.*)$");
// Parse the level and message, create ParsedLogEntry, route to appropriate topics

Console.WriteLine("Logs processed!");
Console.WriteLine();

// ============== Step 3: Search Service ==============
Console.WriteLine("--- Step 3: Building Search Index ---");

// TODO: Create a consumer for parsed-logs topic
// TODO: Build an in-memory index of parsed logs

// TODO: Implement search function
// Hint: IEnumerable<ParsedLogEntry> SearchLogs(string? source, string? level, string? keyword)

Console.WriteLine("Search index built!");
Console.WriteLine();

// ============== Step 4: Metrics Service ==============
Console.WriteLine("--- Step 4: Computing Metrics ---");

// TODO: Compute log counts by source and level
// TODO: Produce metrics to log-metrics topic

Console.WriteLine("Metrics computed!");
Console.WriteLine();

// ============== Step 5: Dashboard ==============
Console.WriteLine("--- Step 5: Dashboard ---");

// TODO: Display log counts by level
// TODO: Display error rate by service
// TODO: Display recent errors

Console.WriteLine();
Console.WriteLine("Log Analytics complete!");

// ============== Domain Models ==============

public record RawLogEntry(string Source, string RawMessage, DateTime Timestamp);

public record ParsedLogEntry(
    string LogId,
    string Source,
    string Level,
    string Message,
    string? Exception,
    Dictionary<string, string> Metadata,
    DateTime Timestamp);

public record LogMetric(
    string Source,
    string Level,
    int Count,
    DateTime WindowStart,
    DateTime WindowEnd);
