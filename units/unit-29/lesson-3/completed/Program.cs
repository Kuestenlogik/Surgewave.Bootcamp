// Surgewave Bootcamp - Unit 11, Lesson 3: Log Analytics
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.3 - Log Analytics ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
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

await using var logProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var random = new Random(42);
var levels = new[] { "INFO", "INFO", "INFO", "INFO", "WARN", "ERROR" }; // Weighted distribution
var totalLogs = 0;

for (var i = 0; i < 50; i++)
{
    var service = services[random.Next(services.Length)];
    var level = levels[random.Next(levels.Length)];
    var message = logMessages[level][random.Next(logMessages[level].Length)];
    var rawLog = new RawLogEntry(service, $"[{level}] {message}", DateTime.UtcNow);

    await logProducer.ProduceAsync("raw-logs",
        Encoding.UTF8.GetBytes(service),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rawLog)));

    totalLogs++;
}

Console.WriteLine($"  Generated {totalLogs} log entries from {services.Length} services");
Console.WriteLine();

// ============== Step 2: Log Processor ==============
Console.WriteLine("--- Step 2: Processing Logs ---");

await using var rawConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "log-processor";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await using var parsedProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var errorProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

rawConsumer.Subscribe("raw-logs");

var levelPattern = new Regex(@"^\[(\w+)\]\s*(.*)$");
var processedCount = 0;
var errorCount = 0;
var timeout = TimeSpan.FromMilliseconds(500);
var deadline = DateTime.UtcNow.AddSeconds(3);

while (DateTime.UtcNow < deadline)
{
    var result = await rawConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var rawLog = JsonSerializer.Deserialize<RawLogEntry>(result.Value)!;
        var match = levelPattern.Match(rawLog.RawMessage);

        var parsed = new ParsedLogEntry(
            Guid.NewGuid().ToString()[..8],
            rawLog.Source,
            match.Success ? match.Groups[1].Value : "UNKNOWN",
            match.Success ? match.Groups[2].Value : rawLog.RawMessage,
            null,
            new Dictionary<string, string>
            {
                ["host"] = Environment.MachineName,
                ["processId"] = Environment.ProcessId.ToString()
            },
            rawLog.Timestamp);

        var key = Encoding.UTF8.GetBytes(parsed.Source);
        var value = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parsed));

        await parsedProducer.ProduceAsync("parsed-logs", key, value);
        processedCount++;

        if (parsed.Level is "ERROR" or "FATAL")
        {
            await errorProducer.ProduceAsync("error-logs", key, value);
            errorCount++;
        }
    }
}

Console.WriteLine($"  Processed {processedCount} logs, {errorCount} errors routed to error-logs topic");
Console.WriteLine();

// ============== Step 3: Search Service ==============
Console.WriteLine("--- Step 3: Building Search Index ---");

await using var indexConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "search-indexer";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

indexConsumer.Subscribe("parsed-logs");

var logIndex = new List<ParsedLogEntry>();
deadline = DateTime.UtcNow.AddSeconds(3);

while (DateTime.UtcNow < deadline)
{
    var result = await indexConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var parsed = JsonSerializer.Deserialize<ParsedLogEntry>(result.Value)!;
        logIndex.Add(parsed);
    }
}

// Search function
IEnumerable<ParsedLogEntry> SearchLogs(string? source = null, string? level = null, string? keyword = null)
{
    var results = logIndex.AsEnumerable();

    if (source != null)
        results = results.Where(l => l.Source == source);
    if (level != null)
        results = results.Where(l => l.Level == level);
    if (keyword != null)
        results = results.Where(l => l.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    return results.OrderByDescending(l => l.Timestamp);
}

Console.WriteLine($"  Indexed {logIndex.Count} log entries");

// Demo searches
Console.WriteLine();
Console.WriteLine("  Search: source=order-service");
var orderLogs = SearchLogs(source: "order-service").Take(3);
foreach (var log in orderLogs)
    Console.WriteLine($"    [{log.Level}] {log.Message}");

Console.WriteLine();
Console.WriteLine("  Search: level=ERROR");
var errors = SearchLogs(level: "ERROR").Take(3);
foreach (var log in errors)
    Console.WriteLine($"    [{log.Source}] {log.Message}");

Console.WriteLine();
Console.WriteLine("  Search: keyword=timeout");
var timeoutLogs = SearchLogs(keyword: "timeout").Take(3);
foreach (var log in timeoutLogs)
    Console.WriteLine($"    [{log.Source}:{log.Level}] {log.Message}");

Console.WriteLine();

// ============== Step 4: Metrics Service ==============
Console.WriteLine("--- Step 4: Computing Metrics ---");

await using var metricsProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var windowStart = DateTime.UtcNow.AddSeconds(-5);
var windowEnd = DateTime.UtcNow;

var metricsList = logIndex
    .GroupBy(l => (l.Source, l.Level))
    .Select(g => new LogMetric(
        g.Key.Source,
        g.Key.Level,
        g.Count(),
        windowStart,
        windowEnd))
    .ToList();

foreach (var metric in metricsList)
{
    await metricsProducer.ProduceAsync("log-metrics",
        Encoding.UTF8.GetBytes($"{metric.Source}-{metric.Level}"),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metric)));
}

Console.WriteLine($"  Computed {metricsList.Count} metrics");
Console.WriteLine();

// ============== Step 5: Dashboard ==============
Console.WriteLine("--- Step 5: Dashboard ---");
Console.WriteLine();
Console.WriteLine("=== LOG ANALYTICS DASHBOARD ===");
Console.WriteLine();

// Log counts by level
Console.WriteLine("Log Counts by Level:");
Console.WriteLine("+----------+-------+");
Console.WriteLine("| Level    | Count |");
Console.WriteLine("+----------+-------+");
foreach (var levelGroup in logIndex.GroupBy(l => l.Level).OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"| {levelGroup.Key,-8} | {levelGroup.Count(),5} |");
}
Console.WriteLine("+----------+-------+");
Console.WriteLine();

// Error rate by service
Console.WriteLine("Error Rate by Service:");
Console.WriteLine("+------------------+-------+--------+-------+");
Console.WriteLine("| Service          | Total | Errors | Rate  |");
Console.WriteLine("+------------------+-------+--------+-------+");
foreach (var service in services)
{
    var total = logIndex.Count(l => l.Source == service);
    var serviceErrors = logIndex.Count(l => l.Source == service && l.Level == "ERROR");
    var rate = total > 0 ? (serviceErrors * 100.0 / total) : 0;
    Console.WriteLine($"| {service,-16} | {total,5} | {serviceErrors,6} | {rate,4:F1}% |");
}
Console.WriteLine("+------------------+-------+--------+-------+");
Console.WriteLine();

// Recent errors
Console.WriteLine("Recent Errors:");
Console.WriteLine("+------------------+------------------------+");
var recentErrors = SearchLogs(level: "ERROR").Take(5);
foreach (var error in recentErrors)
{
    Console.WriteLine($"| {error.Source,-16} | {error.Message,-22} |");
}
Console.WriteLine("+------------------+------------------------+");
Console.WriteLine();

// Log volume by service (histogram)
Console.WriteLine("Log Volume by Service:");
foreach (var service in services)
{
    var count = logIndex.Count(l => l.Source == service);
    var bar = new string('#', count / 2);
    Console.WriteLine($"  {service,-16} [{bar,-25}] {count}");
}

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
