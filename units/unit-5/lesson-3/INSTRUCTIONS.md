# Lesson 7.3: Aggregations & Windowing - Instructions

## Step 1: Group Records

Group records before aggregation:

```csharp
// Group by existing key
var grouped = stream.GroupByKey();

// Group by extracted key
var byCategory = stream.GroupBy((key, value) => value.Category);
```

GroupBy triggers repartitioning if the key changes.

## Step 2: Count

Count records per key:

```csharp
var counts = stream
    .GroupByKey()
    .Count();  // Returns Table<TKey, long>

// Access count changes
counts.ToStream()
    .Peek((key, count) => Console.WriteLine($"{key}: {count}"));
```

## Step 3: Reduce

Combine values of the same type:

```csharp
// Sum numbers
var totals = amounts
    .GroupByKey()
    .Reduce((agg, newValue) => agg + newValue);

// Keep latest
var latest = events
    .GroupByKey()
    .Reduce((old, current) => current);
```

## Step 4: Aggregate

Custom aggregation with different output type:

```csharp
var stats = measurements
    .GroupByKey()
    .Aggregate(
        // Initializer - create empty aggregate
        () => new Stats(0, 0, 0),
        // Aggregator - combine record into aggregate
        (key, value, aggregate) => new Stats(
            aggregate.Count + 1,
            aggregate.Sum + value,
            Math.Max(aggregate.Max, value)
        )
    );
```

## Step 5: Tumbling Windows

Fixed-size, non-overlapping windows:

```csharp
var hourlyCounts = events
    .GroupByKey()
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromHours(1)))
    .Count();

// Result: Table<Windowed<TKey>, long>
hourlyCounts.ToStream()
    .Peek((windowed, count) =>
        Console.WriteLine($"{windowed.Key} [{windowed.Window.StartMs}-{windowed.Window.EndMs}]: {count}"));
```

## Step 6: Hopping Windows

Overlapping windows with advance interval:

```csharp
var movingAverage = values
    .GroupByKey()
    .WindowedBy(
        HoppingWindows
            .Of(TimeSpan.FromMinutes(5))      // 5-minute windows
            .AdvanceBy(TimeSpan.FromMinutes(1))  // Every 1 minute
    )
    .Aggregate(
        () => (Sum: 0.0, Count: 0),
        (key, value, agg) => (agg.Sum + value, agg.Count + 1)
    );
```

## Step 7: Session Windows

Dynamic windows based on inactivity gap:

```csharp
var userSessions = clickstream
    .GroupByKey()  // Key = user ID
    .WindowedBy(SessionWindows.With(TimeSpan.FromMinutes(30)))  // 30-min gap
    .Count();

// Each session represents continuous user activity
// New session starts after 30 minutes of inactivity
```

## Step 8: Grace Period

Allow late data within grace period:

```csharp
var windowed = events
    .GroupByKey()
    .WindowedBy(
        TumblingWindows
            .Of(TimeSpan.FromMinutes(5))
            .Grace(TimeSpan.FromMinutes(1))  // Accept 1 min late
    )
    .Count();
```

**Why Grace Periods Matter:**
In distributed systems, records can arrive out of order due to:
- Network latency and retries
- Producer buffering
- Clock skew between machines
- Partition reassignment delays

Without a grace period, a window closes immediately at the end time, and any
late-arriving records are dropped. With a grace period, the window stays open
for additional time to accept stragglers.

```csharp
// Hopping windows with grace
var movingAvg = values
    .GroupByKey()
    .WindowedBy(
        HoppingWindows
            .Of(TimeSpan.FromMinutes(5))
            .AdvanceBy(TimeSpan.FromMinutes(1))
            .Grace(TimeSpan.FromMinutes(2))
    )
    .Aggregate(...);

// Session windows with grace
var sessions = clicks
    .GroupByKey()
    .WindowedBy(
        SessionWindows
            .With(TimeSpan.FromMinutes(30))
            .Grace(TimeSpan.FromMinutes(5))
    )
    .Count();
```

## Step 9: Monitoring Late Records

Track how many records arrive after the grace period expires:

```csharp
// After running the topology
var metrics = app.Metrics;
Console.WriteLine($"Processed: {metrics.ProcessedRecords}");
Console.WriteLine($"Late (dropped): {metrics.LateRecords}");

// Calculate late record percentage
var latePercentage = metrics.ProcessedRecords > 0
    ? (double)metrics.LateRecords / metrics.ProcessedRecords * 100
    : 0;
Console.WriteLine($"Late percentage: {latePercentage:F2}%");
```

Use this to tune your grace period - if you see many late records, consider
increasing the grace period. If late records are rare, you might reduce it
to lower memory usage and emit results faster.

## Complete Example

```csharp
var builder = new StreamsBuilder("metrics-app");

// Event stream: key=eventType, value=count
var events = builder.Stream<string, int>("events");

// Total counts per event type (non-windowed)
events
    .GroupByKey()
    .Reduce((a, b) => a + b)
    .ToStream()
    .Peek((type, total) => Console.WriteLine($"Total {type}: {total}"));

// Per-minute counts (tumbling window)
events
    .GroupByKey()
    .WindowedBy(TumblingWindows.Of(TimeSpan.FromMinutes(1)))
    .Count()
    .ToStream()
    .Peek((w, count) =>
        Console.WriteLine($"{w.Key} minute {w.Window.StartMs / 60000}: {count}"));

// 5-minute moving sum (hopping window)
events
    .GroupByKey()
    .WindowedBy(
        HoppingWindows
            .Of(TimeSpan.FromMinutes(5))
            .AdvanceBy(TimeSpan.FromMinutes(1))
    )
    .Reduce((a, b) => a + b)
    .ToStream()
    .Peek((w, sum) =>
        Console.WriteLine($"{w.Key} 5min-sum: {sum}"));
```

## Exercises

1. **Event Counter**: Count events by type
2. **Windowed Metrics**: Calculate per-minute and per-hour aggregates
3. **Session Tracking**: Track user sessions with session windows
