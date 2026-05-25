# Lesson 7.3: Aggregations & Windowing

> **Difficulty:** Intermediate | **Duration:** 60 min | **Prerequisites:** Lesson 7.2

## Learning Objectives

- Group streams by key for aggregation
- Use count, reduce, and aggregate operations
- Apply time-based windows (tumbling, hopping, session)
- Understand window semantics and grace periods
- Handle late-arriving records with grace periods
- Monitor late record metrics

## Key Concepts

### Grouping

Before aggregation, group records by key:

```csharp
var grouped = stream.GroupByKey();           // Group by existing key
var grouped = stream.GroupBy((k, v) => v.Category);  // Group by new key
```

### Aggregation Operations

| Operation | Description | Result |
|-----------|-------------|--------|
| `Count()` | Count records per key | Table<Key, long> |
| `Reduce()` | Combine values (same type) | Table<Key, Value> |
| `Aggregate()` | Custom aggregation | Table<Key, Agg> |

### Window Types

```
Tumbling (5 min):
|-------|-------|-------|-------|
0       5      10      15      20

Hopping (5 min, advance 2 min):
|-------|
  |-------|
    |-------|
0   2   4   6   8

Session (5 min gap):
|--event--gap>5min--|--event-event--|
   Session 1            Session 2
```

### Grace Period

In real-world systems, records often arrive out of order due to network delays,
retries, or clock skew. The grace period allows late-arriving records to still
be included in their proper window:

```csharp
// Accept records up to 5 minutes late
var windowed = events
    .GroupByKey()
    .WindowedBy(
        TumblingWindows
            .Of(TimeSpan.FromMinutes(1))
            .Grace(TimeSpan.FromMinutes(5))
    )
    .Count();
```

**How Grace Period Works:**
```
Window: [10:00 - 10:01)
Grace:  5 minutes

Timeline:
10:00 - Window opens
10:01 - Window ends (but stays open for grace period)
10:06 - Window fully closes (grace expires)

Records arriving between 10:01-10:06 for this window
are still processed. Records after 10:06 are dropped.
```

| Window Type | Grace Period Usage |
|-------------|-------------------|
| Tumbling | `TumblingWindows.Of(...).Grace(...)` |
| Hopping | `HoppingWindows.Of(...).AdvanceBy(...).Grace(...)` |
| Session | `SessionWindows.With(...).Grace(...)` |

### Late Record Metrics

Monitor late arrivals using StreamsMetrics:

```csharp
// Access metrics from the streams application
var metrics = app.Metrics;
Console.WriteLine($"Late records dropped: {metrics.LateRecords}");
```

## Exercise

Build a real-time metrics dashboard that:
1. Counts events per type
2. Calculates 1-minute tumbling window aggregates with 30-second grace period
3. Tracks user sessions with 5-minute inactivity gap and 2-minute grace
4. Monitors and logs late record counts

## Time Estimate

~60 minutes
