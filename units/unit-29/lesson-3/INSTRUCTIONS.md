# Lesson 11.3: Log Analytics - Instructions

## Step 1: Define the Domain Models

Create log data types:

```csharp
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
```

## Step 2: Create Log Producers (Service Simulators)

Generate logs from multiple services:

```csharp
var services = new[] { "api-gateway", "user-service", "order-service", "payment-service" };
var levels = new[] { "INFO", "INFO", "INFO", "WARN", "ERROR" }; // Weighted distribution

foreach (var service in services)
{
    var level = levels[random.Next(levels.Length)];
    var message = GenerateLogMessage(service, level);
    var rawLog = new RawLogEntry(service, $"[{level}] {message}", DateTime.UtcNow);

    await producer.ProduceAsync("raw-logs",
        Encoding.UTF8.GetBytes(service),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rawLog)));
}
```

## Step 3: Create the Log Processor

Parse and enrich raw logs:

```csharp
await using var logConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "log-processor";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

logConsumer.Subscribe("raw-logs");

// Parse log format: [LEVEL] message
var levelPattern = new Regex(@"^\[(\w+)\]\s*(.*)$");
var result = await logConsumer.ConsumeAsync(timeout);
if (result != null)
{
    var rawLog = JsonSerializer.Deserialize<RawLogEntry>(result.Value);
    var match = levelPattern.Match(rawLog.RawMessage);

    var parsed = new ParsedLogEntry(
        Guid.NewGuid().ToString()[..8],
        rawLog.Source,
        match.Success ? match.Groups[1].Value : "UNKNOWN",
        match.Success ? match.Groups[2].Value : rawLog.RawMessage,
        null,
        new Dictionary<string, string> { ["host"] = Environment.MachineName },
        rawLog.Timestamp);

    // Route to appropriate topics
    await parsedProducer.ProduceAsync("parsed-logs", key, parsed);

    if (parsed.Level is "ERROR" or "FATAL")
        await errorProducer.ProduceAsync("error-logs", key, parsed);
}
```

## Step 4: Create the Search Service

Index and search logs:

```csharp
// Simple in-memory index (in production, use Elasticsearch)
var logIndex = new List<ParsedLogEntry>();

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
```

## Step 5: Create the Metrics Service

Compute log statistics:

```csharp
// Count logs by source and level
var counts = logIndex
    .GroupBy(l => (l.Source, l.Level))
    .Select(g => new LogMetric(
        g.Key.Source,
        g.Key.Level,
        g.Count(),
        windowStart,
        windowEnd));

foreach (var metric in counts)
{
    await metricsProducer.ProduceAsync("log-metrics",
        Encoding.UTF8.GetBytes($"{metric.Source}-{metric.Level}"),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metric)));
}
```

## Step 6: Create the Dashboard

Display real-time log metrics:

```csharp
Console.WriteLine("=== LOG ANALYTICS DASHBOARD ===");
Console.WriteLine();

// Show error rate by service
Console.WriteLine("Error Rate by Service:");
foreach (var service in services)
{
    var total = metrics.Where(m => m.Source == service).Sum(m => m.Count);
    var errors = metrics.Where(m => m.Source == service && m.Level == "ERROR").Sum(m => m.Count);
    var rate = total > 0 ? (errors * 100.0 / total) : 0;
    Console.WriteLine($"  {service}: {rate:F1}% ({errors}/{total})");
}
```

## Exercises

1. **Basic Platform**: Complete the TODO items in `start/Program.cs`
2. **Exception Parsing**: Extract stack traces from error logs
3. **Log Correlation**: Add trace IDs to correlate logs across services
4. **Retention Policy**: Implement log expiration by age
5. **Full-Text Search**: Implement more sophisticated search capabilities

## Testing Your Implementation

Run the completed solution:

```bash
cd completed
dotnet run
```

Expected output:
- Logs generated from multiple services
- Logs parsed and enriched
- Errors filtered to dedicated topic
- Dashboard showing metrics and error rates
