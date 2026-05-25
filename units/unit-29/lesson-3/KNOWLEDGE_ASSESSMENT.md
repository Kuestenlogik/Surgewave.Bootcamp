# Knowledge Assessment: Lesson 11.3 - Log Analytics

## Multiple Choice

### 1. What is the purpose of the log parsing/enrichment stage?
- [ ] A) To compress log data
- [ ] B) To transform unstructured logs into structured, searchable data
- [ ] C) To delete old logs
- [ ] D) To encrypt sensitive data

<details>
<summary>Answer</summary>
**B)** The parsing stage transforms raw, unstructured log messages into structured data with extracted fields (level, timestamp, source, message) that can be filtered, searched, and analyzed.
</details>

### 2. Which log level should trigger an immediate alert to the on-call team?
- [ ] A) INFO
- [ ] B) WARN
- [ ] C) ERROR
- [ ] D) FATAL

<details>
<summary>Answer</summary>
**D)** FATAL indicates critical failures that require immediate attention. These are typically system-down scenarios that should page on-call personnel. ERROR should also alert, but FATAL is the highest severity.
</details>

### 3. What is the benefit of separating error logs into a dedicated topic?
- [ ] A) Reduces storage costs
- [ ] B) Enables faster alerting and focused analysis without scanning all logs
- [ ] C) Improves log compression
- [ ] D) Reduces network bandwidth

<details>
<summary>Answer</summary>
**B)** A dedicated error-logs topic allows alerting services to subscribe only to errors, enabling immediate notification without processing the full log volume. It also enables focused error analysis.
</details>

## True/False

### 4. Centralized logging provides visibility across all services in a distributed system.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Centralized logging aggregates logs from all services into one place, enabling cross-service debugging, correlation, and system-wide visibility that would be impossible with distributed logs.
</details>

### 5. Raw logs should be deleted immediately after parsing to save storage.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Raw logs should be retained (archived to a raw-logs topic or storage) because parsing might miss information, parsing rules might change, or detailed investigation might require the original data.
</details>

## Code Challenge

### 6. Parse this log line into a structured format:

```
2024-01-15T10:30:45.123Z [ERROR] OrderService - Failed to process order #12345: Payment timeout
```

Define the ParsedLogEntry fields and their values.

<details>
<summary>Answer</summary>
```csharp
var parsed = new ParsedLogEntry
{
    LogId = Guid.NewGuid().ToString(),  // Generated unique ID
    Source = "OrderService",
    Level = "ERROR",
    Message = "Failed to process order #12345: Payment timeout",
    Exception = null,  // No stack trace in this log
    Metadata = new Dictionary<string, string>
    {
        ["orderId"] = "12345",
        ["errorType"] = "PaymentTimeout"
    },
    Timestamp = DateTime.Parse("2024-01-15T10:30:45.123Z")
};

// The regex pattern might look like:
// ^(?<timestamp>\S+)\s+\[(?<level>\w+)\]\s+(?<source>\w+)\s+-\s+(?<message>.+)$
```

Key fields extracted:
- **Timestamp**: 2024-01-15T10:30:45.123Z
- **Level**: ERROR
- **Source**: OrderService
- **Message**: Failed to process order #12345: Payment timeout
- **Metadata**: orderId=12345 (extracted from message)
</details>

### 7. What's missing from this log analytics pipeline?

```csharp
public async Task ProcessLogs()
{
    await foreach (var rawLog in _rawLogConsumer.ConsumeAsync())
    {
        var parsed = ParseLog(rawLog.RawMessage);

        // Send to parsed logs topic
        await _producer.ProduceAsync("parsed-logs", parsed);
    }
}
```

<details>
<summary>Answer</summary>
Several important features are missing:

1. **Error filtering** - No routing of ERROR/FATAL logs to dedicated error topic for alerting
2. **Metrics extraction** - No aggregation of log statistics (error rates, volume, etc.)
3. **Error handling** - No handling of unparseable logs
4. **Source/timestamp enrichment** - Raw log might be missing context

Fix:
```csharp
public async Task ProcessLogs()
{
    await foreach (var rawLog in _rawLogConsumer.ConsumeAsync())
    {
        try
        {
            var parsed = ParseLog(rawLog.RawMessage);

            // Enrich with receive metadata
            parsed = parsed with
            {
                Metadata = parsed.Metadata.Append(
                    new("ingestTime", DateTime.UtcNow.ToString("O")))
                    .ToDictionary(k => k.Key, v => v.Value)
            };

            // 1. Send to parsed logs topic
            await _producer.ProduceAsync("parsed-logs", parsed);

            // 2. Route errors to dedicated topic for alerting
            if (parsed.Level is "ERROR" or "FATAL")
            {
                await _producer.ProduceAsync("error-logs", parsed);
            }

            // 3. Emit metrics
            _logCounter.Add(1,
                new("level", parsed.Level),
                new("source", parsed.Source));
        }
        catch (ParseException ex)
        {
            // 4. Handle unparseable logs
            await _producer.ProduceAsync("failed-logs",
                new { Raw = rawLog, Error = ex.Message });
        }
    }
}
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson
