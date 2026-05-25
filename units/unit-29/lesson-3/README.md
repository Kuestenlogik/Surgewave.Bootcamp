# Lesson 11.3: Log Analytics

> **Difficulty:** Advanced | **Duration:** 60 min | **Prerequisites:** Lesson 11.2

Build a centralized logging platform with search, filtering, and real-time dashboards.

## Objectives

- Design a centralized log aggregation architecture
- Implement log parsing and enrichment
- Create searchable log indexes
- Build real-time log dashboards

## Architecture

```
                           LOG ANALYTICS PLATFORM

    +----------+  +----------+  +----------+  +----------+
    | Service A|  | Service B|  | Service C|  | Service N|
    +----+-----+  +----+-----+  +----+-----+  +----+-----+
         |             |             |             |
         +-------------+------+------+-------------+
                              |
                              v
                    +---------+---------+
                    |    raw-logs       |
                    |      topic        |
                    +---------+---------+
                              |
                    +---------+---------+
                    |  Log Processor    |
                    |  (Parse/Enrich)   |
                    +---------+---------+
                              |
           +------------------+------------------+
           |                  |                  |
           v                  v                  v
    +------+------+    +------+------+    +------+------+
    | parsed-logs |    | error-logs  |    | metrics     |
    |    topic    |    |    topic    |    |    topic    |
    +------+------+    +------+------+    +------+------+
           |                  |                  |
           v                  v                  v
    +------+------+    +------+------+    +------+------+
    |   Search    |    |   Alerts    |    |  Dashboard  |
    |   Service   |    |   Service   |    |   Service   |
    +--------------+   +--------------+   +--------------+
```

## Concepts

### Log Entry Model

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
```

### Log Levels

| Level | Description | Action |
|-------|-------------|--------|
| TRACE | Detailed debugging | Store for debugging |
| DEBUG | Development info | Store for debugging |
| INFO | Normal operations | Standard storage |
| WARN | Potential issues | Alert operations |
| ERROR | Failures | Alert immediately |
| FATAL | Critical failures | Page on-call |

### Topics Structure

- `raw-logs` - Unprocessed log entries
- `parsed-logs` - Structured, enriched logs
- `error-logs` - Filtered error-level logs
- `log-metrics` - Aggregated log statistics

## Key Takeaways

- Centralized logging provides visibility across all services
- Parsing transforms unstructured logs into searchable data
- Real-time filtering enables immediate alerting on errors
- Metrics dashboards show system health at a glance
